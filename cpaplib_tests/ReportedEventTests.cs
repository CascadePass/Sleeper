using cpaplib;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cpaplib_tests
{
    [TestClass]
    public class ReportedEventTests
    {
        [TestMethod]
        public void TimesOverlap_True()
        {
            ReportedEvent
                firstEvent = new() { StartTime = new(2025, 2, 2, 10, 30, 15), Duration = new(1, 2, 3) },
                secondEvent = new() { StartTime = new(2025, 2, 2, 11, 30, 15), Duration = new(2, 3, 4) };

            Assert.IsTrue(ReportedEvent.TimesOverlap(firstEvent, secondEvent));
        }

        [TestMethod]
        public void TimesOverlap_False()
        {
            ReportedEvent
                firstEvent = new() { StartTime = new(2025, 2, 2, 10, 30, 15), Duration = new(1, 2, 3) },
                secondEvent = new() { StartTime = new(2025, 2, 2, 12, 30, 15), Duration = new(2, 3, 4) };

            Assert.IsFalse(ReportedEvent.TimesOverlap(firstEvent, secondEvent));
        }
    }
}
