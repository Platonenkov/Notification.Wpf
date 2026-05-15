using System;
using System.Threading;
using Xunit;

namespace Notification.Core.Tests
{
    public class OperationTimerTests
    {
        [Fact]
        public void Start_Stop_DoesNotThrow()
        {
            using OperationTimer timer = new OperationTimer();
            timer.Start();
            Thread.Sleep(10);
            timer.Stop();
        }

        [Fact]
        public void CalculateOperationTime_ReturnsTimeSpan()
        {
            using OperationTimer timer = new OperationTimer();
            timer.Start();
            Thread.Sleep(50);

            TimeSpan? result = timer.CalculateOperationTime(50, 100);

            Assert.NotNull(result);
            Assert.True(result!.Value.TotalMilliseconds > 0);
        }

        [Fact]
        public void CalculateOperationTime_ZeroIndex_ReturnsNull()
        {
            using OperationTimer timer = new OperationTimer();
            timer.Start();

            TimeSpan? result = timer.CalculateOperationTime(0, 100);

            Assert.Null(result);
        }

        [Fact]
        public void GetStringTime_ReturnsFormattedString()
        {
            using OperationTimer timer = new OperationTimer("Waiting");
            timer.Start();
            Thread.Sleep(50);

            string result = timer.GetStringTime(50, 100);

            Assert.NotNull(result);
        }

        [Fact]
        public void Reset_AllowsRestart()
        {
            using OperationTimer timer = new OperationTimer();
            timer.Start();
            Thread.Sleep(10);
            timer.Reset();
            timer.Start();
            Thread.Sleep(10);
            timer.Stop();
        }

        [Fact]
        public void Restart_ResetsAndStarts()
        {
            using OperationTimer timer = new OperationTimer();
            timer.Start();
            Thread.Sleep(10);
            timer.Restart();
            Thread.Sleep(10);

            TimeSpan? result = timer.CalculateOperationTime(50, 100);

            Assert.NotNull(result);
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            OperationTimer timer = new OperationTimer();
            timer.Start();
            timer.Dispose();
            timer.Dispose();
        }

        [Fact]
        public void Constructor_WithMessage_SetsBaseWaitingMessage()
        {
            using OperationTimer timer = new OperationTimer("Custom message");

            Assert.Equal("Custom message", timer.BaseWaitingMessage);
        }
    }
}
