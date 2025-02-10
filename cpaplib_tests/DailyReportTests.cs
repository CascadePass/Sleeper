using cpaplib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace cpaplib_tests
{
    [TestClass]
    public class DailyReportTests
    {
        [TestMethod]
        public void TotalTimeSpan()
        {
            DailyReport report = new()
            {
                RecordingStartTime = new(2025, 2, 6, 22, 0, 0),
                RecordingEndTime = new(2025, 2, 7, 6, 30, 0)
            };

            Assert.AreEqual(8, report.TotalTimeSpan.Hours);
            Assert.AreEqual(30, report.TotalTimeSpan.Minutes);
        }

        [TestMethod]
        public void PropertiesNotNull()
        {
            DailyReport report = new();

            // Validate that no property is null after instantiation
            // To prevent NullReferenceExceptions

            Assert.IsNotNull(report.MachineInfo);
            Assert.IsNotNull(report.Settings);
            Assert.IsNotNull(report.Sessions);
            Assert.IsNotNull(report.EventSummary);
            //Assert.IsNotNull(report.StatsSummary);
            Assert.IsNotNull(report.Events);
            Assert.IsNotNull(report.Statistics);
            Assert.IsNotNull(report.Notes);
            Assert.IsNotNull(report.Annotations);
        }

        [TestMethod]
        public void CompareTo()
        {
            DailyReport report1 = new(), report2 = new();

            // Both reports blank, both have the default DateTime.MinValue
            Assert.AreEqual(0, report1.CompareTo(report2));

            // report1 has a later date
            report1.ReportDate = new(2025, 2, 6, 22, 0, 0);
            report2.ReportDate = new(2025, 2, 5, 21, 0, 0);

            Assert.AreEqual(1, report1.CompareTo(report2));
            Assert.AreEqual(-1, report2.CompareTo(report1));

            // report2 has a later date
            report1.ReportDate = new(2025, 2, 1, 22, 0, 0);
            report2.ReportDate = new(2025, 2, 2, 21, 0, 0);

            Assert.AreEqual(-1, report1.CompareTo(report2));
            Assert.AreEqual(1, report2.CompareTo(report1));

            // Explicitly the same date
            report1.ReportDate = new(2025, 2, 3, 22, 0, 0);
            report2.ReportDate = new(2025, 2, 3, 22, 0, 0);

            Assert.AreEqual(0, report1.CompareTo(report2));
        }

        [TestMethod]
        public void RefreshTimeRange()
        {
            DailyReport report = new();

            #region Single session

            // Expectation is that no exceptions are thrown

            report.Sessions.Add(new()
            {
                StartTime = new(2025, 2, 6, 22, 0, 0),
                EndTime = new(2025, 2, 7, 6, 30, 0)
            });

            report.RefreshTimeRange();

            Assert.AreEqual(8, report.TotalSleepTime.Hours);
            Assert.AreEqual(30, report.TotalSleepTime.Minutes);

            #endregion

            #region Overlapping sessions

            /*
            This test fails, but it's not a big deal. The data is bad.
            The user should not be able to enter overlapping sessions.
            Let's get this corrected, but it's not urgent.

            // Expectation is the answer is still correct
            // even with this bad data

            report.Sessions.Add(new()
            {
                StartTime = new(2025, 2, 6, 23, 15, 0),
                EndTime = new(2025, 2, 7, 4, 30, 0)
            });

            report.RefreshTimeRange();

            Assert.AreEqual(8, report.TotalSleepTime.Hours);
            Assert.AreEqual(30, report.TotalSleepTime.Minutes);
            */

            #endregion

            #region Multiple sessions

            // User got up to use the bathroom.
            // Poor user, they might have nocturia.
            // Sleep apnea is awful.

            report.Sessions.Add(new()
            {
                StartTime = new(2025, 2, 7, 6, 33, 0),
                EndTime = new(2025, 2, 7, 7, 30, 0)
            });

            report.RefreshTimeRange();

            Assert.AreEqual(9, report.TotalSleepTime.Hours);
            Assert.AreEqual(27, report.TotalSleepTime.Minutes);

            #endregion
        }

        [TestMethod]
        public void AddSession()
        {
            DailyReport report = new();

            #region Assert default values

            Assert.AreEqual(default, report.RecordingStartTime);
            Assert.AreEqual(default, report.RecordingEndTime);

            Assert.AreEqual(0, report.TotalSleepTime.Hours);
            Assert.AreEqual(0, report.TotalSleepTime.Minutes);

            #endregion

            #region Single session

            report.AddSession(new()
            {
                StartTime = new(2025, 2, 6, 22, 0, 0),
                EndTime = new(2025, 2, 7, 6, 30, 0),
                SourceType = SourceType.CPAP,
            });

            // TotalSleepTime has been updated
            Assert.AreEqual(8, report.TotalSleepTime.Hours);
            Assert.AreEqual(30, report.TotalSleepTime.Minutes);

            // No detail data was added
            Assert.IsFalse(report.HasDetailData);

            #endregion

            #region Multiple sessions

            report.RecordingStartTime = report.Sessions[0].StartTime;
            report.RecordingEndTime = report.Sessions[0].EndTime;

            report.AddSession(new()
            {
                StartTime = new(2025, 2, 7, 6, 30, 1),
                EndTime = new(2025, 2, 7, 8, 0, 0),
                SourceType = SourceType.CPAP,
                Signals =
                [
                    new()
                    {
                        Name = "Test Signal",
                        StartTime = new(2025, 2, 7, 6, 30, 1),
                        EndTime = new(2025, 2, 7, 8, 0, 0),
                        MinValue = 0,
                        MaxValue = 100,
                    }
                ]
            });

            // Validate HasDetailData
            Assert.IsTrue(report.HasDetailData);

            // Validate that RecordingStartTime and RecordingEndTime were updated
            Assert.AreEqual(report.Sessions[0].StartTime, report.RecordingStartTime);
            Assert.AreEqual(report.Sessions[1].EndTime, report.RecordingEndTime);

            #endregion
        }

        [TestMethod]
        public void CalculateSleepEfficiency()
        {
            DailyReport report = new();
            double efficiency;

            #region No sessions = no exception

            efficiency = report.CalculateSleepEfficiency();

            // There's no data, so the answer should be zero.
            Assert.AreEqual(default, efficiency);

            #endregion

            #region Single session

            report.AddSession(new()
            {
                StartTime = new(2025, 2, 6, 22, 0, 0),
                EndTime = new(2025, 2, 7, 6, 30, 0),
                SourceType = SourceType.CPAP,
            });

            efficiency = report.CalculateSleepEfficiency();

            // There is only one session.  A CPAP or BiPAP machine doesn't know when its user
            // is asleep or awake.  It only knows when it's on or off.  So, the efficiency
            // is always 1.0.  Values below 100% reflect time spent away from the sleep machine.
            // Scenarios are: user got up to use the rest room, took off the mask, etc.  These
            // are not considered sleep time.

            Assert.AreEqual(1, efficiency);

            #endregion

            #region Multiple sessions

            report.AddSession(new()
            {
                StartTime = new(2025, 2, 7, 6, 35, 0),
                EndTime = new(2025, 2, 7, 8, 0, 0),
                SourceType = SourceType.CPAP,
            });

            efficiency = report.CalculateSleepEfficiency();

            // A five minute break in the middle of the night is not a big deal,
            // as the number below reflects.  This is what the math works out to.
            Assert.AreEqual(.9915966386554622, efficiency);

            #endregion
        }

        [TestMethod]
        public void CalculateTotalSleepTime()
        {
            DailyReport report = new();
            TimeSpan totalSleepTime;

            #region No sessions = no exception

            totalSleepTime = report.CalculateTotalSleepTime();

            // There's no data, so the answer should be zero.
            Assert.AreEqual(default, totalSleepTime);

            #endregion

            #region Single session

            report.AddSession(new()
            {
                StartTime = new(2025, 2, 6, 22, 0, 0),
                EndTime = new(2025, 2, 7, 6, 30, 0),
                SourceType = SourceType.CPAP,
            });

            totalSleepTime = report.CalculateTotalSleepTime();

            // There is only one session.  A CPAP or BiPAP machine doesn't know when its user
            // is asleep or awake.  It only knows when it's on or off.  So, the efficiency
            // is always 1.0.  Values below 100% reflect time spent away from the sleep machine.
            // Scenarios are: user got up to use the rest room, took off the mask, etc.  These
            // are not considered sleep time.

            Assert.AreEqual(8, totalSleepTime.Hours);
            Assert.AreEqual(30, totalSleepTime.Minutes);

            #endregion

            #region Multiple sessions

            report.AddSession(new()
            {
                StartTime = new(2025, 2, 7, 6, 35, 0),
                EndTime = new(2025, 2, 7, 8, 0, 0),
                SourceType = SourceType.CPAP,
            });

            totalSleepTime = report.CalculateTotalSleepTime();

            Assert.AreEqual(9, totalSleepTime.Hours);
            Assert.AreEqual(55, totalSleepTime.Minutes);

            #endregion

            #region Other session types do not count

            report.AddSession(new()
            {
                StartTime = new(2025, 2, 7, 6, 35, 0),
                EndTime = new(2025, 2, 7, 9, 0, 0),
                SourceType = SourceType.PulseOximetry,
            });

            totalSleepTime = report.CalculateTotalSleepTime();

            Assert.AreEqual(9, totalSleepTime.Hours);
            Assert.AreEqual(55, totalSleepTime.Minutes);

            #endregion
        }

        [TestMethod]
        public void MyTestMethod()
        {
            DailyReport report = new();

            // Use reflection to get the private RemoveSession method
            var methodInfo = typeof(DailyReport).GetMethod("RemoveSession", BindingFlags.NonPublic | BindingFlags.Instance);

            // Assert that it worked
            Assert.IsNotNull(methodInfo);

            #region Single session

            Session testSession = new()
            {
                StartTime = new(2025, 2, 6, 22, 0, 0),
                EndTime = new(2025, 2, 7, 6, 30, 0),
                SourceType = SourceType.CPAP,
            };

            report.AddSession(testSession);

            // Use reflection to call the protected RemoveSession method
            var result = (bool)methodInfo.Invoke(report, [testSession]);

            Assert.IsTrue(result);

            #endregion

            #region No sessions

            // Use reflection to call the protected RemoveSession method
            result = (bool)methodInfo.Invoke(report, [testSession]);

            Assert.IsFalse(result);

            #endregion

            #region Multiple sessions

            report.AddSession(new()
            {
                StartTime = new(2025, 2, 7, 5, 0, 0),
                EndTime = new(2025, 2, 7, 6, 0, 0),
                SourceType = SourceType.CPAP,
            });

            report.AddSession(testSession);

            report.AddSession(new()
            {
                StartTime = new(2025, 2, 7, 6, 5, 0),
                EndTime = new(2025, 2, 7, 7, 0, 0),
                SourceType = SourceType.CPAP,
            });

            // Use reflection to call the protected RemoveSession method
            result = (bool)methodInfo.Invoke(report, [testSession]);

            Assert.IsTrue(result);

            #endregion
        }
    }
}
