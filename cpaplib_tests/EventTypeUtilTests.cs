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
    public class EventTypeUtilTests
    {
        [TestMethod]
        public void FromName()
        {
            Assert.AreEqual(EventType.RERA, EventTypeUtil.FromName("Arousal"));
            Assert.AreEqual(EventType.RERA, EventTypeUtil.FromName("RERA"));
            Assert.AreEqual(EventType.Hypopnea, EventTypeUtil.FromName("Hypopnea"));
            Assert.AreEqual(EventType.ObstructiveApnea, EventTypeUtil.FromName("Obstructive Apnea"));
            Assert.AreEqual(EventType.ClearAirway, EventTypeUtil.FromName("Central Apnea"));
            Assert.AreEqual(EventType.ClearAirway, EventTypeUtil.FromName("Clear Airway"));
            Assert.AreEqual(EventType.PeriodicBreathing, EventTypeUtil.FromName("Periodic Breathing"));
            Assert.AreEqual(EventType.UnclassifiedApnea, EventTypeUtil.FromName("Unclassified"));
            Assert.AreEqual(EventType.RecordingStarts, EventTypeUtil.FromName("Recording starts"));
            Assert.AreEqual(EventType.RecordingEnds, EventTypeUtil.FromName("Recording ends"));
        }

        [TestMethod]
        public void ToName()
        {
            Assert.AreEqual("RERA", EventType.RERA.ToName());
            Assert.AreEqual("Hypopnea", EventType.Hypopnea.ToName());
            Assert.AreEqual("Obstructive Apnea", EventType.ObstructiveApnea.ToName());
            Assert.AreEqual("Central Apnea", EventType.ClearAirway.ToName());
            Assert.AreEqual("Periodic Breathing", EventType.PeriodicBreathing.ToName());
            Assert.AreEqual("Unclassified", EventType.UnclassifiedApnea.ToName());
            Assert.AreEqual("Recording starts", EventType.RecordingStarts.ToName());
            Assert.AreEqual("Recording ends", EventType.RecordingEnds.ToName());
        }
    }
}
