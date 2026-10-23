using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Jay.AnimStateEvent.Tests
{
    // Animator 없이 Hub / Track / TimedEvent의 로직만 검증합니다.
    // Edit Mode에서는 Hub.Awake가 호출되지 않으므로 Register를 직접 부릅니다(= 외부에서 트랙을 주입하는 경로).
    public class AnimStateEventLogicTests
    {
        private readonly List<Object> created = new List<Object>();
        private AnimStateEventHub hub;
        private List<string> fired;

        [SetUp]
        public void SetUp()
        {
            var go = new GameObject("HubUnderTest");
            created.Add(go);
            hub = go.AddComponent<AnimStateEventHub>();
            fired = new List<string>();
            hub.OnEventFired += (track, timedEvent) => fired.Add(timedEvent.EventName);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var obj in created)
            {
                if (obj != null) Object.DestroyImmediate(obj);
            }
            created.Clear();
        }

        private AnimStateEventTrack CreateTrack(string key, params TimedEvent[] events)
        {
            var track = ScriptableObject.CreateInstance<AnimStateEventTrack>();
            SetPrivate(track, "trackKey", key);
            SetPrivate(track, "events", events);
            created.Add(track);
            return track;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }

        private AnimStateEventTrack CreateDefaultTrack(string key = "Attack")
        {
            return CreateTrack(key, new TimedEvent("Hit", 0.3f, "light"), new TimedEvent("End", 0.9f, "end"));
        }

        // ───────────── TimedEvent ─────────────

        [Test, Category("Spec")]
        public void TimedEvent_Constructor_StoresValues()
        {
            var timedEvent = new TimedEvent("Hit", 0.3f, "light");

            Assert.AreEqual("Hit", timedEvent.EventName);
            Assert.AreEqual(0.3f, timedEvent.NormalizedTime);
            Assert.AreEqual("light", timedEvent.Parameter);
        }

        [Test, Category("Spec")]
        public void TimedEvent_SetNormalizedTime_AssignsValue()
        {
            var timedEvent = new TimedEvent("Hit", 0.3f);

            timedEvent.NormalizedTime = 0.7f;

            Assert.AreEqual(0.7f, timedEvent.NormalizedTime);
        }

        [TestCase(1.5f, 1f)]
        [TestCase(-0.5f, 0f)]
        [Category("Spec")]
        public void TimedEvent_SetNormalizedTime_ClampsTo01(float input, float expected)
        {
            var timedEvent = new TimedEvent("Hit", 0.3f);

            timedEvent.NormalizedTime = input;

            Assert.AreEqual(expected, timedEvent.NormalizedTime);
        }

        [Test, Category("Spec")]
        public void TimedEvent_Clone_IsIndependentCopy()
        {
            var original = new TimedEvent("Hit", 0.3f, "light");

            var clone = original.Clone();

            Assert.AreNotSame(original, clone);
            Assert.AreEqual(original.EventName, clone.EventName);
            Assert.AreEqual(original.NormalizedTime, clone.NormalizedTime);
            Assert.AreEqual(original.Parameter, clone.Parameter);
        }

        // ───────────── Register / Unregister ─────────────

        [Test, Category("Spec")]
        public void Register_CopiesDefaultEvents_AsClones()
        {
            var track = CreateDefaultTrack();

            hub.Register(track);
            var events = hub.GetEvents(track);

            Assert.AreEqual(2, events.Count);
            Assert.AreNotSame(track.Events[0], events[0], "Hub는 에셋 이벤트를 복제해서 들고 있어야 함");
            hub.RemoveEvent(track, "Hit");
            Assert.AreEqual(2, track.Events.Count, "Hub 쪽 편집이 트랙(에셋) 데이터를 바꾸면 안 됨");
        }

        [Test, Category("Spec")]
        public void Register_Twice_DoesNotDuplicateEvents()
        {
            var track = CreateDefaultTrack();

            hub.Register(track);
            hub.Register(track);

            Assert.AreEqual(2, hub.GetEvents(track).Count);
        }

        [Test, Category("Spec")]
        public void Register_NullEventsArray_RegistersEmptyTrack()
        {
            var track = CreateTrack("Empty", null);

            Assert.DoesNotThrow(() => hub.Register(track));
            Assert.AreEqual(0, hub.GetEvents(track).Count);
            Assert.IsTrue(hub.TryGetTrack("Empty", out _));
        }

        [Test, Category("Spec")]
        public void Register_EmptyKey_IsNotIndexed_ButStillRuns()
        {
            var track = CreateTrack("", new TimedEvent("Hit", 0.3f));

            hub.Register(track);
            hub.EnterTrack(track);
            hub.UpdateTrack(track, 0.5f);

            Assert.IsFalse(hub.TryGetTrack("", out _));
            CollectionAssert.AreEqual(new[] { "Hit" }, fired);
        }

        [Test, Category("Spec")]
        public void Register_DuplicateKey_KeepsFirstTrack_AndWarns()
        {
            var first = CreateDefaultTrack();
            var second = CreateDefaultTrack();

            hub.Register(first);
            LogAssert.Expect(LogType.Warning, new Regex("같은 TrackKey가 이미 있습니다"));
            hub.Register(second);

            Assert.AreSame(first, hub.GetTrackOrNull("Attack"));
            Assert.AreEqual(2, hub.GetEvents(second).Count, "키 등록은 실패해도 트랙 상태는 등록됨");
        }

        [Test, Category("Spec")]
        public void Unregister_RemovesStateAndKey()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);

            hub.Unregister(track);
            hub.AddEvent(track, new TimedEvent("Injected", 0.1f));

            Assert.IsNull(hub.GetTrackOrNull("Attack"));
            Assert.AreEqual(0, hub.GetEvents(track).Count);
        }

        [Test, Category("Spec")]
        public void Unregister_DuplicateKeyTrack_DoesNotRemoveFirstTracksKey()
        {
            var first = CreateDefaultTrack();
            var second = CreateDefaultTrack();
            hub.Register(first);
            LogAssert.Expect(LogType.Warning, new Regex("같은 TrackKey가 이미 있습니다"));
            hub.Register(second);

            hub.Unregister(second);

            Assert.AreSame(first, hub.GetTrackOrNull("Attack"));
        }

        [Test, Category("Spec")]
        public void Unregister_ThenRegisterAnotherTrackWithSameKey_Works()
        {
            var first = CreateDefaultTrack();
            var second = CreateDefaultTrack();
            hub.Register(first);

            hub.Unregister(first);
            hub.Register(second);

            Assert.AreSame(second, hub.GetTrackOrNull("Attack"));
        }

        // ───────────── Enter / Update ─────────────

        [Test, Category("Spec")]
        public void UpdateTrack_FiresWhenTimeReached_OncePerEnter()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);
            hub.EnterTrack(track);

            hub.UpdateTrack(track, 0.1f);
            Assert.IsEmpty(fired);

            hub.UpdateTrack(track, 0.3f);
            hub.UpdateTrack(track, 0.5f);
            CollectionAssert.AreEqual(new[] { "Hit" }, fired);

            hub.UpdateTrack(track, 0.95f);
            hub.UpdateTrack(track, 0.99f);
            CollectionAssert.AreEqual(new[] { "Hit", "End" }, fired);
        }

        [Test, Category("Spec")]
        public void UpdateTrack_FrameSkip_FiresAllPassedEvents()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);
            hub.EnterTrack(track);

            hub.UpdateTrack(track, 1f);

            CollectionAssert.AreEqual(new[] { "Hit", "End" }, fired);
        }

        [Test, Category("Spec")]
        public void UpdateTrack_LoopingState_DoesNotRefireWithoutReEnter()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);
            hub.EnterTrack(track);

            hub.UpdateTrack(track, 0.5f);
            hub.UpdateTrack(track, 1.4f); // 루프 2회차 0.4 지점

            CollectionAssert.AreEqual(new[] { "Hit", "End" }, fired);
        }

        [Test, Category("Spec")]
        public void EnterTrack_ResetsFiredEvents()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);

            hub.EnterTrack(track);
            hub.UpdateTrack(track, 1f);
            hub.EnterTrack(track);
            hub.UpdateTrack(track, 1f);

            CollectionAssert.AreEqual(new[] { "Hit", "End", "Hit", "End" }, fired);
        }

        [Test, Category("Spec")]
        public void EnterTrack_UnregisteredTrack_AutoRegisters()
        {
            var track = CreateDefaultTrack();

            hub.EnterTrack(track);
            hub.UpdateTrack(track, 1f);

            Assert.AreSame(track, hub.GetTrackOrNull("Attack"));
            CollectionAssert.AreEqual(new[] { "Hit", "End" }, fired);
        }

        [Test, Category("Spec")]
        public void UpdateTrack_UnregisteredTrack_DoesNothing()
        {
            var track = CreateDefaultTrack();

            Assert.DoesNotThrow(() => hub.UpdateTrack(track, 1f));
            Assert.IsEmpty(fired);
        }

        // ───────────── 외부 동적 주입 (AddEvent / RemoveEvent / ClearEvents / SetEventTime) ─────────────

        [Test, Category("Spec")]
        public void AddEvent_BeforeEnter_FiresWithDefaults()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);

            hub.AddEvent(track, new TimedEvent("Injected", 0.5f, "dyn"));
            hub.EnterTrack(track);
            hub.UpdateTrack(track, 1f);

            CollectionAssert.AreEqual(new[] { "Hit", "End", "Injected" }, fired);
        }

        [Test, Category("Spec")]
        public void AddEvent_MidState_WithPassedTime_FiresOnNextUpdate()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);
            hub.EnterTrack(track);
            hub.UpdateTrack(track, 0.5f);

            hub.AddEvent(track, new TimedEvent("Late", 0.1f));
            hub.UpdateTrack(track, 0.51f);

            CollectionAssert.AreEqual(new[] { "Hit", "Late" }, fired);
        }

        [Test, Category("Spec")]
        public void AddEvent_IsKeptAcrossReEnter()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);
            hub.AddEvent(track, new TimedEvent("Injected", 0.5f));

            hub.EnterTrack(track);
            hub.UpdateTrack(track, 1f);
            fired.Clear();
            hub.EnterTrack(track);
            hub.UpdateTrack(track, 1f);

            CollectionAssert.Contains(fired, "Injected");
        }

        [Test, Category("Spec")]
        public void RemoveEvent_RemovesAndStopsFiring()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);

            Assert.IsTrue(hub.RemoveEvent(track, "Hit"));
            Assert.IsFalse(hub.RemoveEvent(track, "Hit"));
            Assert.IsFalse(hub.RemoveEvent(track, "Unknown"));
            hub.EnterTrack(track);
            hub.UpdateTrack(track, 1f);

            CollectionAssert.AreEqual(new[] { "End" }, fired);
        }

        [Test, Category("Spec")]
        public void RemoveEvent_UnregisteredTrack_ReturnsFalse()
        {
            Assert.IsFalse(hub.RemoveEvent(CreateDefaultTrack(), "Hit"));
        }

        [Test, Category("Spec")]
        public void ClearEvents_ThenAddEvent_OnlyNewEventFires()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);

            hub.ClearEvents(track);
            hub.AddEvent(track, new TimedEvent("Injected", 0.5f));
            hub.EnterTrack(track);
            hub.UpdateTrack(track, 1f);

            CollectionAssert.AreEqual(new[] { "Injected" }, fired);
        }

        [Test, Category("Spec")]
        public void SetEventTime_ChangesStoredTime()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);

            Assert.IsTrue(hub.SetEventTime(track, "Hit", 0.95f));

            Assert.AreEqual(0.95f, hub.GetEvents(track)[0].NormalizedTime);
        }

        [Test, Category("Spec")]
        public void SetEventTime_ChangesFiringOrder()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);
            hub.SetEventTime(track, "Hit", 0.95f);

            hub.EnterTrack(track);
            hub.UpdateTrack(track, 0.5f);

            Assert.IsEmpty(fired, "Hit를 0.95로 옮겼으므로 0.5 시점엔 아무것도 발생하지 않아야 함");
        }

        [Test, Category("Spec")]
        public void SetEventTime_UnknownEventOrTrack_ReturnsFalse()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);

            Assert.IsFalse(hub.SetEventTime(track, "Unknown", 0.5f));
            Assert.IsFalse(hub.SetEventTime(CreateDefaultTrack("Other"), "Hit", 0.5f));
        }

        [Test, Category("Spec")]
        public void OnEventFired_AddEventInsideCallback_FiresInSameUpdate()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);
            hub.OnEventFired += (t, e) =>
            {
                if (e.EventName == "Hit") hub.AddEvent(t, new TimedEvent("Chained", 0.2f));
            };

            hub.EnterTrack(track);
            hub.UpdateTrack(track, 0.5f);

            CollectionAssert.AreEqual(new[] { "Hit", "Chained" }, fired);
        }

        // ───────────── 위험 시나리오 (방어 코드가 기대되는 경계 사례) ─────────────

        [Test, Category("Risk")]
        public void AddEvent_BeforeTrackRegistered_IsNotLost()
        {
            // 외부 주입 코드가 Hub보다 먼저 트랙 참조를 얻은 경우(컨트롤러 늦은 할당 등)
            var track = CreateDefaultTrack();

            hub.AddEvent(track, new TimedEvent("Injected", 0.5f));
            hub.EnterTrack(track); // 여기서 자동 등록됨
            hub.UpdateTrack(track, 1f);

            CollectionAssert.Contains(fired, "Injected",
                "EnterTrack은 자동 등록하는데 AddEvent는 미등록 트랙에 조용히 무시됨 → 주입 유실");
        }

        [Test, Category("Risk")]
        public void OnEventFired_RemovingSelfInsideCallback_DoesNotSkipNextEvent()
        {
            // '한 번만 받고 지우기' 패턴
            var track = CreateTrack("Attack", new TimedEvent("A", 0.1f), new TimedEvent("B", 0.2f));
            hub.Register(track);
            hub.OnEventFired += (t, e) =>
            {
                if (e.EventName == "A") hub.RemoveEvent(t, "A");
            };

            hub.EnterTrack(track);
            hub.UpdateTrack(track, 0.5f);

            CollectionAssert.AreEqual(new[] { "A", "B" }, fired,
                "콜백 안에서 현재 이벤트를 지우면 리스트가 당겨져 같은 프레임의 B가 건너뛰어짐");
        }

        [Test, Category("Risk")]
        public void AddEvent_Null_IsIgnored()
        {
            var track = CreateDefaultTrack();
            hub.Register(track);

            Assert.DoesNotThrow(() =>
            {
                hub.AddEvent(track, null);
                hub.EnterTrack(track);
                hub.UpdateTrack(track, 1f);
                hub.SetEventTime(track, "End", 0.5f);
            });
        }

        [Test, Category("Risk")]
        public void GetTrackOrNull_NullKey_ReturnsNull()
        {
            Assert.DoesNotThrow(() => Assert.IsNull(hub.GetTrackOrNull(null)));
        }

        [Test, Category("Risk")]
        public void Register_Null_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => hub.Register(null));
        }
    }
}
