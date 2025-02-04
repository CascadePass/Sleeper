﻿using cpap_app.Helpers;
using cpaplib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpaplib_tests
{
    [TestClass]
    public class DateTimeTests
    {
        [TestMethod]
        public void CanDetectDaylightSavingTime()
        {
            DateTime testDateTime = new(2024, 5, 15, 14, 31, 0, DateTimeKind.Local);
            var isDaylightSavings = DateUtil.IsDaylightSavingTime(testDateTime);
            Assert.IsTrue(isDaylightSavings, $"Date '{testDateTime}' ({testDateTime.Kind}), isDaylightSavings = {isDaylightSavings}.");

            testDateTime = new DateTime(2024, 2, 15, 14, 31, 0, DateTimeKind.Local);
            isDaylightSavings = DateUtil.IsDaylightSavingTime(testDateTime);
            Assert.IsFalse(isDaylightSavings, $"Date '{testDateTime}' ({testDateTime.Kind}), isDaylightSavings = {isDaylightSavings}.");
        }

        [TestMethod]
        public void DateTimeFromGoogleAPI()
        {
            var startTime = DateTime.UnixEpoch.ToLocalTime().AddMilliseconds(1700028990000);
            var endTime = DateTime.UnixEpoch.ToLocalTime().AddMilliseconds(1700055270000);

            var compareEndTime = new DateTime(1970, 1, 1).ToLocalTime().AddMilliseconds(1700055270000);

            Assert.AreEqual(endTime, compareEndTime);

            Debug.WriteLine($"Start: {startTime},    End: {endTime},    Duration: {endTime - startTime}");
        }

        [TestMethod]
        public void DateUtil_Max()
        {
            DateTime first = new(1977, 12, 21), last = new(2025, 2, 2);
            var result = DateUtil.Max(first, last);

            Assert.AreEqual(last, result);
        }

        [TestMethod]
        public void DateUtil_Min()
        {
            DateTime first = new(1977, 12, 21), last = new(2025, 2, 2);
            var result = DateUtil.Min(first, last);

            Assert.AreEqual(first, result);
        }
    }
}
