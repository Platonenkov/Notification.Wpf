using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media;
using Notification.Wpf.Controls;
using Xunit;
using WpfNotification = Notification.Wpf.Controls.Notification;

namespace Notification.Wpf.Tests
{
    /// <summary>
    /// Regression coverage for review suspicious point #1: the working-tree change removed the
    /// early ClearBindingsRecursive(this) that 2c103dc placed at the top of Close() (before the
    /// animation wait, while the visual tree is still intact), leaving only the post-animation
    /// Cleanup() pass. NotificationArea removes the notification from _items inside the
    /// NotificationClosed handler — i.e. BEFORE Cleanup() runs — so if the subtree is already
    /// detached by then, the recursive cleanup could miss it and the ReflectPropertyDescriptor
    /// event dictionary would keep strongly referencing NotificationContent (the leak chain
    /// 2c103dc fixed). This test asserts both the direct effect (no residual bindings on the
    /// template subtree) and the end-to-end effect (NotificationContent is collectable).
    /// </summary>
    // Shares static Areas/_window state with the other UI tests — same collection keeps them
    // serialized; StaTask.ResetSharedWpfState resets the state before every test.
    [Collection("NotificationUiState")]
    public class CloseBindingCleanupTests
    {
        private readonly ITestOutputHelper _output;

        public CloseBindingCleanupTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void Close_clears_template_bindings_and_content_is_collectable()
        {
            StaTask.Run(async () =>
            {
                // All strong references to content/notification/window live only inside the
                // helper's async state machine, which completes before the GC phase below —
                // so a live contentRef after GC proves an external (leak) reference.
                (WeakReference contentRef, int residualBindings, bool contentNulled, int treeSize)
                    = await ShowCloseAndReleaseAsync();

                _output.WriteLine($"[diag] template subtree nodes: {treeSize}");
                _output.WriteLine($"[diag] notification.Content nulled by Cleanup: {contentNulled}");
                _output.WriteLine($"[diag] residual bindings on subtree after Close: {residualBindings}");

                Assert.True(contentNulled,
                    "Cleanup() must have run (notification.Content == null) before the binding check.");
                Assert.True(treeSize > 0,
                    "The template subtree must be traversable after Close — otherwise the binding " +
                    "probe is vacuous and cannot detect the regression it targets.");

                // Core assertion for suspicious point #1: the post-animation Cleanup() must reach
                // the same bindings the removed early pass used to clear. If detaching the
                // notification from _items before Cleanup() makes VisualTreeHelper traversal miss
                // the subtree, residualBindings > 0 here.
                Assert.Equal(0, residualBindings);

                // End-to-end: NotificationContent must become collectable. A surviving reference
                // means some chain (e.g. ReflectPropertyDescriptor event dictionary through a
                // leftover binding) still pins the content — the steady-growth leak 2c103dc fixed.
                for (int i = 0; i < 3 && contentRef.IsAlive; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    await Task.Delay(100);
                }
                Assert.False(contentRef.IsAlive,
                    "NotificationContent must be collectable after Close; a live reference means the " +
                    "post-animation cleanup missed bindings on the detached template subtree " +
                    "(leak regression from removing the early ClearBindingsRecursive).");
            }, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Shows one notification in a plain host window, closes it through the real Close()
        /// pipeline (animation wait → NotificationClosed → _items.Remove → Cleanup()), then
        /// releases every strong reference and reports probes taken before the state machine ends.
        /// </summary>
        private static async Task<(WeakReference contentRef, int residualBindings, bool contentNulled, int treeSize)>
            ShowCloseAndReleaseAsync()
        {
            var content = new NotificationContent
            {
                Title = "leak-probe",
                Message = "message"
            };
            var area = new NotificationArea();
            var window = new Window
            {
                Width = 400,
                Height = 300,
                Content = area,
                ShowInTaskbar = false
            };

            try
            {
                window.Show();

                DateTime loadDeadline = DateTime.UtcNow.AddSeconds(5);
                while (!area.IsLoaded && DateTime.UtcNow < loadDeadline)
                    await Task.Delay(50);
                Assert.True(area.IsLoaded, "NotificationArea must be loaded before Show.");

                var closedTcs = new TaskCompletionSource<object>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                WpfNotification notification = null;

                area.Show(content, TimeSpan.FromSeconds(30), onClick: null, onClose: null,
                    CloseOnClick: false, ShowXbtn: true,
                    onCreated: n =>
                    {
                        notification = n;
                        n.NotificationClosed += (_, _) => closedTcs.TrySetResult(null);
                    });
                Assert.NotNull(notification);

                // Wait for the template (and thus the bound subtree) to be applied.
                DateTime tplDeadline = DateTime.UtcNow.AddSeconds(5);
                while (VisualTreeHelper.GetChildrenCount(notification) == 0 &&
                       DateTime.UtcNow < tplDeadline)
                    await Task.Delay(50);

                notification.Close();

                // NotificationClosed fires after the animation wait; Cleanup() runs synchronously
                // right after RaiseEvent returns, and the TCS continuation (RunContinuationsAsynchronously)
                // is queued behind it on the same dispatcher — so by the time we resume, Cleanup
                // has completed. The extra pump absorbs any trailing dispatcher work.
                await closedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await Task.Delay(200);

                int treeSize = CountNodes(notification);
                int residual = CountBindingsInTree(notification);
                bool contentNulled = notification.Content == null;
                var contentRef = new WeakReference(content);
                return (contentRef, residual, contentNulled, treeSize);
            }
            finally
            {
                window.Close();
                // Unloaded (→ NotificationManager.RemoveArea) is dispatched asynchronously;
                // let it flush so the static Areas list is clean for the next test.
                await Task.Delay(200);
            }
        }

        /// <summary>
        /// Second probe for suspicious point #1, covering the customization path documented in the
        /// sample (implicit Style + ContentTemplateSelector / per-notification ContentTemplate):
        /// a custom DataTemplate with a nested visual structure (StackPanel > Border > TextBlock)
        /// bound to a plain POCO content. Probe strategy (learned from diagnostics): bindings
        /// instantiated by a template live in the template-value layer, NOT the local-value layer —
        /// GetLocalValueEnumerator/ReadLocalValue cannot see them (and ClearAllBindings cannot
        /// clear them either; they die with the template subtree). So the test proves the binding
        /// chain three ways: (1) pre-close the bindings must evaluate (Text == "label"/"detail"),
        /// (2) post-close ContentTemplate=null must have unloaded the generated subtree, (3) the
        /// POCO content must be collectable — the end-to-end leak assertion.
        /// </summary>
        [Fact]
        public void Close_with_custom_content_template_clears_bindings_and_content_is_collectable()
        {
            StaTask.Run(async () =>
            {
                // All strong references to content/notification/window live only inside the
                // helper's async state machine, which completes before the GC phase below —
                // so a live contentRef after GC proves an external (leak) reference.
                (WeakReference contentRef, bool bindingsEvaluated, int textBlocksAfterClose,
                 bool contentNulled, bool templateNulled)
                    = await ShowCloseCustomTemplateAsync();

                _output.WriteLine($"[diag] bindings evaluated before Close: {bindingsEvaluated}");
                _output.WriteLine($"[diag] Content/ContentTemplate nulled by Cleanup: {contentNulled}/{templateNulled}");
                _output.WriteLine($"[diag] template-generated TextBlocks after Close: {textBlocksAfterClose}");

                // Pre-close: the custom template must have instantiated AND its bindings must have
                // evaluated against the POCO — otherwise nothing downstream proves anything.
                Assert.True(bindingsEvaluated,
                    "The custom DataTemplate must instantiate and evaluate its bindings before Close " +
                    "(TextBlock.Text must read \"label\" and \"detail\") — the template did not apply.");

                Assert.True(contentNulled, "Cleanup() must null notification.Content.");
                Assert.True(templateNulled, "Cleanup() must null notification.ContentTemplate.");

                // Post-close: ContentTemplate=null must make the ContentPresenter drop the
                // generated subtree — that unloading is what severs element → binding → source.
                Assert.Equal(0, textBlocksAfterClose);

                // End-to-end: the POCO content must become collectable. A surviving reference
                // means some chain (binding expression, event dictionary, retained subtree) still
                // pins the content — the steady-growth leak 2c103dc fixed.
                for (int i = 0; i < 3 && contentRef.IsAlive; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    await Task.Delay(100);
                }
                Assert.False(contentRef.IsAlive,
                    "Custom-template content must be collectable after Close; a live reference " +
                    "means the custom DataTemplate subtree (or its bindings) survived Cleanup() " +
                    "(leak regression from removing the early ClearBindingsRecursive).");
            }, TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Same real Close() pipeline as the default-template test, but the notification carries a
        /// custom DataTemplate (parsed from XAML, nested structure) over a plain POCO content.
        /// All strong references stay inside this state machine and are released on return.
        /// </summary>
        private async Task<(WeakReference contentRef, bool bindingsEvaluated, int textBlocksAfterClose,
            bool contentNulled, bool templateNulled)>
            ShowCloseCustomTemplateAsync()
        {
            var content = new CustomProbeContent();

            // Nested structure (StackPanel > Border > TextBlock) proves the recursive cleanup
            // descends below the top level of the content template.
            const string templateXaml = """
                <DataTemplate xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation">
                    <StackPanel>
                        <TextBlock Text="{Binding Label}"/>
                        <Border>
                            <TextBlock Text="{Binding Detail}"/>
                        </Border>
                    </StackPanel>
                </DataTemplate>
                """;
            var template = (DataTemplate)XamlReader.Parse(templateXaml);

            var area = new NotificationArea();
            var window = new Window
            {
                Width = 400,
                Height = 300,
                Content = area,
                ShowInTaskbar = false
            };

            try
            {
                window.Show();

                DateTime loadDeadline = DateTime.UtcNow.AddSeconds(5);
                while (!area.IsLoaded && DateTime.UtcNow < loadDeadline)
                    await Task.Delay(50);
                Assert.True(area.IsLoaded, "NotificationArea must be loaded before Show.");

                var closedTcs = new TaskCompletionSource<object>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                WpfNotification notification = null;

                area.Show(content, TimeSpan.FromSeconds(30), onClick: null, onClose: null,
                    CloseOnClick: false, ShowXbtn: true,
                    onCreated: n =>
                    {
                        notification = n;
                        // Custom template path: applied per notification, before it enters the tree.
                        n.ContentTemplate = template;
                        n.NotificationClosed += (_, _) => closedTcs.TrySetResult(null);
                    });
                Assert.NotNull(notification);

                // Wait until the content template has generated its subtree AND the bindings have
                // evaluated against the POCO (Text reads the property values). Template bindings
                // live in the template-value layer, invisible to GetLocalValueEnumerator — the
                // evaluated Text is the observable proof that the binding chain is live.
                bool bindingsEvaluated = false;
                DateTime tplDeadline = DateTime.UtcNow.AddSeconds(5);
                while (DateTime.UtcNow < tplDeadline)
                {
                    if (TemplateBindingsEvaluated(notification))
                    {
                        bindingsEvaluated = true;
                        break;
                    }
                    await Task.Delay(50);
                }

                notification.Close();

                await closedTcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
                await Task.Delay(200);

                // After Cleanup() nulls ContentTemplate, the ContentPresenter must have dropped
                // the generated subtree — count any surviving template-generated TextBlocks.
                int textBlocksAfterClose = CountTemplateTextBlocks(notification);
                bool contentNulled = notification.Content == null;
                bool templateNulled = notification.ContentTemplate == null;
                var contentRef = new WeakReference(content);
                return (contentRef, bindingsEvaluated, textBlocksAfterClose, contentNulled, templateNulled);
            }
            finally
            {
                window.Close();
                await Task.Delay(200);
            }
        }

        /// <summary>True when both template TextBlocks show the evaluated POCO property values —
        /// i.e. the custom DataTemplate instantiated and its bindings resolved.</summary>
        private static bool TemplateBindingsEvaluated(WpfNotification notification)
        {
            ContentPresenter presenter = FindDescendant<ContentPresenter>(notification);
            if (presenter == null)
                return false;
            bool sawLabel = false, sawDetail = false;
            foreach (TextBlock tb in FindAllDescendants<TextBlock>(presenter))
            {
                if (tb.Text == "label") sawLabel = true;
                else if (tb.Text == "detail") sawDetail = true;
            }
            return sawLabel && sawDetail;
        }

        /// <summary>Counts TextBlocks under the ContentPresenter that carry the template's
        /// evaluated values — survivors after Close mean the generated subtree was retained.</summary>
        private static int CountTemplateTextBlocks(WpfNotification notification)
        {
            ContentPresenter presenter = FindDescendant<ContentPresenter>(notification);
            if (presenter == null)
                return 0;
            int count = 0;
            foreach (TextBlock tb in FindAllDescendants<TextBlock>(presenter))
            {
                if (tb.Text == "label" || tb.Text == "detail")
                    count++;
            }
            return count;
        }

        /// <summary>Plain POCO content for the custom-template probe: no DependencyObject base,
        /// so the only references to it from the UI are the generated subtree's binding
        /// expressions — exactly the chain under test.</summary>
        private sealed class CustomProbeContent
        {
            public string Label { get; set; } = "label";
            public string Detail { get; set; } = "detail";
        }

        /// <summary>Counts BindingExpressionBase entries in the local-value layer of the element
        /// and its visual subtree — exactly what ClearAllBindings (used by ClearBindingsRecursive)
        /// is able to clear, so the probe is symmetric with the production cleanup.</summary>
        private static int CountBindingsInTree(DependencyObject root)
        {
            int count = 0;
            var stack = new Stack<DependencyObject>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                DependencyObject d = stack.Pop();
                LocalValueEnumerator enumerator = d.GetLocalValueEnumerator();
                while (enumerator.MoveNext())
                {
                    if (enumerator.Current.Value is BindingExpressionBase)
                        count++;
                }

                if (d is Visual)
                {
                    int childCount = VisualTreeHelper.GetChildrenCount(d);
                    for (int i = 0; i < childCount; i++)
                        stack.Push(VisualTreeHelper.GetChild(d, i));
                }
            }
            return count;
        }

        private static int CountNodes(DependencyObject root)
        {
            int count = 0;
            var stack = new Stack<DependencyObject>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                DependencyObject d = stack.Pop();
                count++;
                if (d is Visual)
                {
                    int childCount = VisualTreeHelper.GetChildrenCount(d);
                    for (int i = 0; i < childCount; i++)
                        stack.Push(VisualTreeHelper.GetChild(d, i));
                }
            }
            return count;
        }

        /// <summary>First descendant of the given type in the visual subtree, or null.</summary>
        private static T FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            var stack = new Stack<DependencyObject>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                DependencyObject d = stack.Pop();
                if (d is T match)
                    return match;
                if (d is Visual)
                {
                    int childCount = VisualTreeHelper.GetChildrenCount(d);
                    for (int i = 0; i < childCount; i++)
                        stack.Push(VisualTreeHelper.GetChild(d, i));
                }
            }
            return null;
        }

        /// <summary>All descendants of the given type in the visual subtree (root excluded).</summary>
        private static List<T> FindAllDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            var result = new List<T>();
            var stack = new Stack<DependencyObject>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                DependencyObject d = stack.Pop();
                if (d is Visual)
                {
                    int childCount = VisualTreeHelper.GetChildrenCount(d);
                    for (int i = 0; i < childCount; i++)
                    {
                        DependencyObject child = VisualTreeHelper.GetChild(d, i);
                        if (child is T match)
                            result.Add(match);
                        stack.Push(child);
                    }
                }
            }
            return result;
        }
    }
}
