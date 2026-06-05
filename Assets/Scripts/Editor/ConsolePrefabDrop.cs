#if UNITY_EDITOR && !DONT_AUTOCREATE_EVENTSYS
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SPTr.Editor
{
    [InitializeOnLoad]
    public static class ConsolePrefabDrop
    {
        private const string TargetPrefabName = "Console System UI";
        static ConsolePrefabDrop()
        {
            ObjectChangeEvents.changesPublished += OnChangesPublished;
        }

        private static void OnChangesPublished(ref ObjectChangeEventStream stream)
        {
            for (int i = 0; i < stream.length; ++i)
            {
                if (stream.GetEventType(i) == ObjectChangeKind.CreateGameObjectHierarchy)
                {
                    stream.GetCreateGameObjectHierarchyEvent(i, out var eventArgs);

                    // 생성된 오브젝트 가져오기
                    GameObject spawnedGO = EditorUtility.InstanceIDToObject(eventArgs.instanceId) as GameObject;

                    if (spawnedGO != null && spawnedGO.name.StartsWith(TargetPrefabName))
                    {
                        // 씬에 이미 EventSystem이 있는지 확인
                        EventSystem existingEventSystem = Object.FindFirstObjectByType<EventSystem>();

                        if (existingEventSystem == null)
                        {
                            ProcessCombinedCreation(spawnedGO);
                        }
                    }
                }
            }
        }

        private static void ProcessCombinedCreation(GameObject targetGO)
        {
            // 현재 진행 중인 Undo 그룹을 하나로 묶기
            int undoGroupIndex = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Spawn Prefab with EventSystem");

            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemGO.AddComponent<StandaloneInputModule>();
#endif
            // 새로 만든 EventSystem을 생성 Undo에 등록 (동일 그룹으로 묶임)
            Undo.RegisterCreatedObjectUndo(eventSystemGO, "Create EventSystem");

            // 새로 생성된 EventSystem을 프리팹과 같은 씬/스테이지로 이동
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(eventSystemGO, targetGO.scene);

            // Undo 그룹 닫기 (이후의 작업과 분리)
            Undo.CollapseUndoOperations(undoGroupIndex);
        }
    }
}
#endif
