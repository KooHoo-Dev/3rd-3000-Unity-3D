using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

namespace Jay.AnimStateEvent.Tests
{
    // 실제 Animator + AnimatorController로 Play Mode에서 돌려보는 통합 테스트.
    // Edit Mode 테스트 안에서 EnterPlayMode로 진입합니다.
    // 도메인 리로드 때 이터레이터의 클로저 객체가 사라지므로, 람다를 쓰는 본문은 진입 후 별도 이터레이터(_Body)에서 실행합니다.
    // Run In Background가 꺼져 있으면 에디터가 포커스를 잃을 때 플레이어 루프가 멈추므로, Animator를 직접 Update(Step)으로 진행시킵니다.
    public class AnimStateEventPlayModeTests
    {
        private const string OriginalSceneKey = "AnimStateEventTests.OriginalScene";
        private const float Step = 1f / 60f;
        private const int MaxSteps = 300;          // 5초 분량
        private const int StepsPerFrame = 10;

        private static readonly List<Animator> actors = new List<Animator>();

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            AnimStateEventTestAssets.Delete();

            var originalScene = SessionState.GetString(OriginalSceneKey, "");
            SessionState.EraseString(OriginalSceneKey);
            if (string.IsNullOrEmpty(originalScene) == false)
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
        }

        [UnityTearDown]
        public IEnumerator UnityTearDown()
        {
            actors.Clear();
            if (Application.isPlaying) yield return new ExitPlayMode();
        }

        // Edit Mode에서 호출: 에셋 준비 + 빈 씬으로 교체(사용자 씬 스크립트가 같이 돌지 않도록)
        private static void PrepareInEditMode()
        {
            AnimStateEventTestAssets.Ensure();

            if (string.IsNullOrEmpty(SessionState.GetString(OriginalSceneKey, "")))
                SessionState.SetString(OriginalSceneKey, EditorSceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static Animator CreateActor(string name, RuntimeAnimatorController controller, bool withHub)
        {
            var go = new GameObject(name);
            var animator = go.AddComponent<Animator>();
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = controller;
            if (withHub) go.AddComponent<AnimStateEventHub>();
            actors.Add(animator);
            return animator;
        }

        private static List<string> Record(AnimStateEventHub hub)
        {
            var names = new List<string>();
            hub.OnEventFired += (track, timedEvent) => names.Add(timedEvent.EventName);
            return names;
        }

        private static void StepActors()
        {
            foreach (var animator in actors)
            {
                if (animator != null && animator.isActiveAndEnabled) animator.Update(Step);
            }
        }

        private static IEnumerator Tick()
        {
            StepActors();
            yield return null;
        }

        private static IEnumerator WaitFor(Func<bool> condition)
        {
            for (int step = 0; step < MaxSteps && condition() == false; step++)
            {
                StepActors();
                if (step % StepsPerFrame == StepsPerFrame - 1) yield return null;
            }
        }

        private static bool IsIn(Animator animator, string state)
        {
            return animator.IsInTransition(0) == false && animator.GetCurrentAnimatorStateInfo(0).IsName(state);
        }

        // ───────────── 기본 흐름 ─────────────

        [UnityTest, Category("Spec")]
        public IEnumerator Basic_EventsFireOnceInOrder_WithParameterAndTrackKey()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Basic_EventsFireOnceInOrder_WithParameterAndTrackKey_Body();
        }

        private IEnumerator Basic_EventsFireOnceInOrder_WithParameterAndTrackKey_Body()
        {
            var animator = CreateActor("Actor", AnimStateEventTestAssets.LoadA(), true);
            var hub = animator.GetComponent<AnimStateEventHub>();
            var received = new List<string>();
            hub.OnEventFired += (track, e) => received.Add($"{track.TrackKey}:{e.EventName}:{e.Parameter}");
            yield return Tick();

            animator.SetTrigger("Attack");
            yield return WaitFor(() => received.Count >= 2);
            yield return WaitFor(() => IsIn(animator, "Idle"));

            CollectionAssert.AreEqual(new[] { "Attack:Hit:light", "Attack:End:end" }, received);
        }

        [UnityTest, Category("Spec")]
        public IEnumerator Replay_EventsFireAgainOnEachEnter()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Replay_EventsFireAgainOnEachEnter_Body();
        }

        private IEnumerator Replay_EventsFireAgainOnEachEnter_Body()
        {
            var animator = CreateActor("Actor", AnimStateEventTestAssets.LoadA(), true);
            var fired = Record(animator.GetComponent<AnimStateEventHub>());
            yield return Tick();

            animator.SetTrigger("Attack");
            yield return WaitFor(() => fired.Count >= 2);
            yield return WaitFor(() => IsIn(animator, "Idle"));
            animator.SetTrigger("Attack");
            yield return WaitFor(() => fired.Count >= 4);

            CollectionAssert.AreEqual(new[] { "Hit", "End", "Hit", "End" }, fired);
        }

        [UnityTest, Category("Spec")]
        public IEnumerator Awake_RegistersTracksFromAnimator_ByKey()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Awake_RegistersTracksFromAnimator_ByKey_Body();
        }

        private IEnumerator Awake_RegistersTracksFromAnimator_ByKey_Body()
        {
            var animator = CreateActor("Actor", AnimStateEventTestAssets.LoadA(), true);
            var hub = animator.GetComponent<AnimStateEventHub>();
            yield return Tick();

            Assert.IsTrue(hub.TryGetTrack(AnimStateEventTestAssets.TrackKey, out var track));
            Assert.AreSame(animator.GetBehaviours<AnimStateEventTrack>()[0], track);
        }

        // ───────────── 외부 동적 주입 ─────────────

        [UnityTest, Category("Spec")]
        public IEnumerator Inject_AddEventFromOutside_FiresInTimeOrder_AndAssetUntouched()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Inject_AddEventFromOutside_FiresInTimeOrder_AndAssetUntouched_Body();
        }

        private IEnumerator Inject_AddEventFromOutside_FiresInTimeOrder_AndAssetUntouched_Body()
        {
            var animator = CreateActor("Actor", AnimStateEventTestAssets.LoadA(), true);
            var hub = animator.GetComponent<AnimStateEventHub>();
            var fired = Record(hub);
            yield return Tick();

            // 다른 오브젝트(예: 무기/스킬 시스템)가 키로 트랙을 찾아 이벤트를 꽂는 상황
            var track = hub.GetTrackOrNull(AnimStateEventTestAssets.TrackKey);
            Assert.IsNotNull(track);
            hub.AddEvent(track, new TimedEvent("Injected", 0.6f, "dyn"));

            animator.SetTrigger("Attack");
            yield return WaitFor(() => fired.Count >= 3);

            CollectionAssert.AreEqual(new[] { "Hit", "Injected", "End" }, fired);
            Assert.AreEqual(2, track.Events.Count, "런타임 주입이 트랙 에셋 데이터를 바꾸면 안 됨");
        }

        [UnityTest, Category("Spec")]
        public IEnumerator Inject_SetEventTimeFromOutside_ChangesFiringOrder()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Inject_SetEventTimeFromOutside_ChangesFiringOrder_Body();
        }

        private IEnumerator Inject_SetEventTimeFromOutside_ChangesFiringOrder_Body()
        {
            var animator = CreateActor("Actor", AnimStateEventTestAssets.LoadA(), true);
            var hub = animator.GetComponent<AnimStateEventHub>();
            var fired = Record(hub);
            yield return Tick();

            var track = hub.GetTrackOrNull(AnimStateEventTestAssets.TrackKey);
            Assert.IsTrue(hub.SetEventTime(track, "Hit", 0.95f));

            animator.SetTrigger("Attack");
            yield return WaitFor(() => fired.Count >= 2);

            CollectionAssert.AreEqual(new[] { "End", "Hit" }, fired);
        }

        [UnityTest, Category("Spec")]
        public IEnumerator Inject_RemoveEventFromOutside_DuringPlayback()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Inject_RemoveEventFromOutside_DuringPlayback_Body();
        }

        private IEnumerator Inject_RemoveEventFromOutside_DuringPlayback_Body()
        {
            var animator = CreateActor("Actor", AnimStateEventTestAssets.LoadA(), true);
            var hub = animator.GetComponent<AnimStateEventHub>();
            var fired = Record(hub);
            yield return Tick();

            animator.SetTrigger("Attack");
            yield return WaitFor(() => fired.Count >= 1);
            hub.RemoveEvent(hub.GetTrackOrNull(AnimStateEventTestAssets.TrackKey), "End");
            yield return WaitFor(() => IsIn(animator, "Idle"));

            CollectionAssert.AreEqual(new[] { "Hit" }, fired);
        }

        [UnityTest, Category("Spec")]
        public IEnumerator Inject_HubAddedByAddComponentAtRuntime_BeforeStateEnter()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Inject_HubAddedByAddComponentAtRuntime_BeforeStateEnter_Body();
        }

        private IEnumerator Inject_HubAddedByAddComponentAtRuntime_BeforeStateEnter_Body()
        {
            var animator = CreateActor("Actor", AnimStateEventTestAssets.LoadA(), false);
            yield return Tick();
            yield return null;

            var hub = animator.gameObject.AddComponent<AnimStateEventHub>();
            var fired = Record(hub);
            Assert.IsTrue(hub.TryGetTrack(AnimStateEventTestAssets.TrackKey, out var track));
            hub.AddEvent(track, new TimedEvent("Injected", 0.6f));

            animator.SetTrigger("Attack");
            yield return WaitFor(() => fired.Count >= 3);

            CollectionAssert.AreEqual(new[] { "Hit", "Injected", "End" }, fired);
        }

        [UnityTest, Category("Spec")]
        public IEnumerator Inject_TwoActorsShareController_InjectionIsIsolated()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Inject_TwoActorsShareController_InjectionIsIsolated_Body();
        }

        private IEnumerator Inject_TwoActorsShareController_InjectionIsIsolated_Body()
        {
            var controller = AnimStateEventTestAssets.LoadA();
            var a = CreateActor("A", controller, true);
            var b = CreateActor("B", controller, true);
            var hubA = a.GetComponent<AnimStateEventHub>();
            var hubB = b.GetComponent<AnimStateEventHub>();
            var firedA = Record(hubA);
            var firedB = Record(hubB);
            yield return Tick();

            hubA.AddEvent(hubA.GetTrackOrNull(AnimStateEventTestAssets.TrackKey), new TimedEvent("OnlyA", 0.6f));
            a.SetTrigger("Attack");
            b.SetTrigger("Attack");
            yield return WaitFor(() => firedA.Count >= 3 && firedB.Count >= 2);
            yield return WaitFor(() => IsIn(a, "Idle") && IsIn(b, "Idle"));

            CollectionAssert.AreEqual(new[] { "Hit", "OnlyA", "End" }, firedA);
            CollectionAssert.AreEqual(new[] { "Hit", "End" }, firedB);
        }

        [UnityTest, Category("Spec")]
        public IEnumerator Inject_ControllerAssignedAfterHub_DefaultEventsStillFire()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Inject_ControllerAssignedAfterHub_DefaultEventsStillFire_Body();
        }

        private IEnumerator Inject_ControllerAssignedAfterHub_DefaultEventsStillFire_Body()
        {
            var animator = CreateActor("Actor", null, true);
            var fired = Record(animator.GetComponent<AnimStateEventHub>());
            yield return Tick();

            animator.runtimeAnimatorController = AnimStateEventTestAssets.LoadA();
            yield return Tick();
            animator.SetTrigger("Attack");
            yield return WaitFor(() => fired.Count >= 2);

            CollectionAssert.AreEqual(new[] { "Hit", "End" }, fired);
        }

        // ───────────── 위험 시나리오 ─────────────

        [UnityTest, Category("Risk")]
        public IEnumerator Risk_ControllerAssignedAfterHub_TrackLookupByKey()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Risk_ControllerAssignedAfterHub_TrackLookupByKey_Body();
        }

        private IEnumerator Risk_ControllerAssignedAfterHub_TrackLookupByKey_Body()
        {
            var animator = CreateActor("Actor", null, true);
            var hub = animator.GetComponent<AnimStateEventHub>();
            yield return Tick();

            animator.runtimeAnimatorController = AnimStateEventTestAssets.LoadA();
            yield return Tick();

            Assert.IsTrue(hub.TryGetTrack(AnimStateEventTestAssets.TrackKey, out _),
                "Awake 이후에 컨트롤러가 들어오면, 해당 상태에 한 번 진입하기 전까지 키로 트랙을 찾을 수 없음 → 사전 주입 불가");
        }

        [UnityTest, Category("Risk")]
        public IEnumerator Risk_ControllerSwap_KeyPointsToLiveTrack()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Risk_ControllerSwap_KeyPointsToLiveTrack_Body();
        }

        private IEnumerator Risk_ControllerSwap_KeyPointsToLiveTrack_Body()
        {
            var animator = CreateActor("Actor", AnimStateEventTestAssets.LoadA(), true);
            var hub = animator.GetComponent<AnimStateEventHub>();
            var fired = Record(hub);
            yield return Tick();
            var oldTrack = hub.GetTrackOrNull(AnimStateEventTestAssets.TrackKey);

            animator.runtimeAnimatorController = AnimStateEventTestAssets.LoadB();
            yield return Tick();
            var liveTrack = animator.GetBehaviours<AnimStateEventTrack>()[0];
            Assert.AreNotSame(oldTrack, liveTrack, "전제: 컨트롤러 교체 시 SMB 인스턴스가 새로 만들어짐");

            animator.SetTrigger("Attack");
            yield return WaitFor(() => fired.Contains("B_Hit"));
            CollectionAssert.Contains(fired, "B_Hit", "새 컨트롤러의 기본 이벤트는 자동 등록으로 발생해야 함");

            Assert.AreSame(liveTrack, hub.GetTrackOrNull(AnimStateEventTestAssets.TrackKey),
                $"교체 후에도 키가 옛 트랙(파괴됨={oldTrack == null})을 가리킴 → 키로 주입한 이벤트가 유실됨");
        }

        [UnityTest, Category("Risk")]
        public IEnumerator Risk_DeactivateReactivate_InjectedEventSurvives()
        {
            // 오브젝트 풀링(SetActive 토글) 상황
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Risk_DeactivateReactivate_InjectedEventSurvives_Body();
        }

        private IEnumerator Risk_DeactivateReactivate_InjectedEventSurvives_Body()
        {
            var animator = CreateActor("Actor", AnimStateEventTestAssets.LoadA(), true);
            var hub = animator.GetComponent<AnimStateEventHub>();
            var fired = Record(hub);
            yield return Tick();
            var before = hub.GetTrackOrNull(AnimStateEventTestAssets.TrackKey);
            hub.AddEvent(before, new TimedEvent("Injected", 0.6f));

            animator.gameObject.SetActive(false);
            yield return Tick();
            animator.gameObject.SetActive(true);
            yield return Tick();
            var after = animator.GetBehaviours<AnimStateEventTrack>()[0];

            animator.SetTrigger("Attack");
            yield return WaitFor(() => fired.Count >= 3);

            CollectionAssert.AreEqual(new[] { "Hit", "Injected", "End" }, fired,
                $"SetActive 토글 후 SMB 인스턴스 동일={ReferenceEquals(before, after)}");
        }

        [UnityTest, Category("Risk")]
        public IEnumerator Risk_HubAddedWhileStatePlaying_RemainingEventsFire()
        {
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Risk_HubAddedWhileStatePlaying_RemainingEventsFire_Body();
        }

        private IEnumerator Risk_HubAddedWhileStatePlaying_RemainingEventsFire_Body()
        {
            var animator = CreateActor("Actor", AnimStateEventTestAssets.LoadA(), false);
            yield return Tick();

            LogAssert.Expect(LogType.Error, new Regex("has no AnimStateEventHub attached"));
            animator.SetTrigger("Attack");
            yield return WaitFor(() => IsIn(animator, "Attack"));
            var enteredAt = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            Assume.That(enteredAt, Is.LessThan(0.3f), "Hit(0.3) 이전에 Hub를 붙여야 의미 있는 테스트");

            var fired = Record(animator.gameObject.AddComponent<AnimStateEventHub>());
            yield return WaitFor(() => IsIn(animator, "Idle"));

            CollectionAssert.AreEqual(new[] { "Hit", "End" }, fired,
                "Track이 OnStateEnter에서만 Hub를 찾으므로, 재생 중에 붙은 Hub는 이번 재생분을 전부 놓침");
        }

        [UnityTest, Category("Risk")]
        public IEnumerator Risk_SelfTransitionReEnter_HitDoesNotFireEarly()
        {
            // 연타/재사격처럼 같은 상태로 블렌드 재진입하는 상황 (AnyState → Attack, 0.25 블렌드)
            PrepareInEditMode();
            yield return new EnterPlayMode();
            yield return Risk_SelfTransitionReEnter_HitDoesNotFireEarly_Body();
        }

        private IEnumerator Risk_SelfTransitionReEnter_HitDoesNotFireEarly_Body()
        {
            var animator = CreateActor("Actor", AnimStateEventTestAssets.LoadA(), true);
            var hub = animator.GetComponent<AnimStateEventHub>();
            var log = new List<string>();
            var destTimes = new List<float>();
            hub.OnEventFired += (track, e) =>
            {
                var inTransition = animator.IsInTransition(0);
                var info = inTransition ? animator.GetNextAnimatorStateInfo(0) : animator.GetCurrentAnimatorStateInfo(0);
                log.Add(e.EventName);
                destTimes.Add(info.normalizedTime);
            };
            yield return Tick();

            animator.SetTrigger("Attack");
            yield return WaitFor(() => IsIn(animator, "Attack") && animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.5f);
            Assume.That(log, Is.EqualTo(new[] { "Hit" }));

            animator.SetTrigger("ReAttack");
            yield return WaitFor(() => log.Count >= 2);

            Assert.AreEqual("Hit", log[1]);
            Assert.GreaterOrEqual(destTimes[1], 0.25f,
                $"재진입한 상태 기준 {destTimes[1]:0.00} 지점에서 Hit 발생 — 이전 재생(소스 상태)의 OnStateUpdate가 리셋된 기록으로 즉시 발사");
        }
    }
}
