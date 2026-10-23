using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Jay.AnimStateEvent.Tests
{
    // 통합 테스트용 임시 AnimatorController 에셋을 만들고 지웁니다.
    // Play Mode 진입 시 도메인 리로드가 일어나므로 경로 상수로만 상태를 공유합니다.
    internal static class AnimStateEventTestAssets
    {
        public const string Folder = "Assets/__AnimStateEventTestTemp";
        public const string PathA = Folder + "/ControllerA.controller";
        public const string PathB = Folder + "/ControllerB.controller";
        public const float ClipLength = 1f;
        public const string TrackKey = "Attack";

        public static void Ensure()
        {
            if (AssetDatabase.IsValidFolder(Folder) == false)
                AssetDatabase.CreateFolder("Assets", "__AnimStateEventTestTemp");

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(PathA) == null)
                Build(PathA, new TimedEvent("Hit", 0.3f, "light"), new TimedEvent("End", 0.9f, "end"));

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(PathB) == null)
                Build(PathB, new TimedEvent("B_Hit", 0.5f, "b"));
        }

        public static RuntimeAnimatorController LoadA() => AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PathA);
        public static RuntimeAnimatorController LoadB() => AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PathB);

        public static void Delete()
        {
            if (AssetDatabase.IsValidFolder(Folder)) AssetDatabase.DeleteAsset(Folder);
        }

        // Idle(기본) --Attack 트리거--> Attack --exitTime 1--> Idle
        // AnyState --ReAttack 트리거(자기 자신 전이 허용, 0.25 블렌드)--> Attack
        private static void Build(string path, params TimedEvent[] events)
        {
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("ReAttack", AnimatorControllerParameterType.Trigger);

            var clip = new AnimationClip { name = "TestClip" };
            AnimationUtility.SetEditorCurve(clip,
                EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"),
                AnimationCurve.Linear(0f, 0f, ClipLength, 0f));
            AssetDatabase.AddObjectToAsset(clip, controller);

            var stateMachine = controller.layers[0].stateMachine;
            var idle = stateMachine.AddState("Idle");
            idle.motion = clip;
            var attack = stateMachine.AddState("Attack");
            attack.motion = clip;
            stateMachine.defaultState = idle;

            var toAttack = idle.AddTransition(attack);
            toAttack.hasExitTime = false;
            toAttack.duration = 0f;
            toAttack.AddCondition(AnimatorConditionMode.If, 0f, "Attack");

            var toIdle = attack.AddTransition(idle);
            toIdle.hasExitTime = true;
            toIdle.exitTime = 1f;
            toIdle.duration = 0f;

            var reAttack = stateMachine.AddAnyStateTransition(attack);
            reAttack.hasExitTime = false;
            reAttack.duration = 0.25f;
            reAttack.canTransitionToSelf = true;
            reAttack.AddCondition(AnimatorConditionMode.If, 0f, "ReAttack");

            var track = attack.AddStateMachineBehaviour<AnimStateEventTrack>();
            var serialized = new SerializedObject(track);
            serialized.FindProperty("trackKey").stringValue = TrackKey;
            var array = serialized.FindProperty("events");
            array.arraySize = events.Length;
            for (int i = 0; i < events.Length; i++)
            {
                var element = array.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("eventName").stringValue = events[i].EventName;
                element.FindPropertyRelative("normalizedTime").floatValue = events[i].NormalizedTime;
                element.FindPropertyRelative("parameter").stringValue = events[i].Parameter;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();

            // SaveAssets()는 프로젝트의 다른 dirty 에셋까지 저장하므로, 테스트 에셋만 저장합니다
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);
        }
    }
}
