using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using SPTr.DeveloperConsole;

namespace SPTr.Demo
{
    public static class RigidBodyCMD
    {
        public const string COLOR_ERROR = "#F78181";
        public const string COLOR_INFO = "#F7BE81";
        public const string COLOR_VALUE = "#F5DA81";

        [ConCmd("rb_gravity", "강체의 전역 중력값")]
        public static void SetGravity(float force) => Physics.gravity = Vector3.down * force;

        public static bool GlobalInterPolate = false;

        private static Camera _mainCam;

        [ConCmd("rb_create_box", "시점이 향한 곳에 상자를 생성합니다. \n- rb_create_box <x 스케일>, <y 스케일>, <z 스케일>", ExecFlag.CHEAT)]
        public static void CreateBox(string optArg)
        {
            var args = optArg.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (args.Length != 3)
            {
                Debug.LogError($"<color={COLOR_ERROR}>박스 생성 오류, 인자를 확인해주세요.</color>");
                return;
            }

            if (!TryGetMaincam(out Camera useCam))
                return;

            var maxSize = 0f;
            var parseValues = new float[3];

            for (int i = 0; i < args.Length; i++)
            {
                parseValues[i] = float.Parse(args[i]);
                if (maxSize < parseValues[i])
                    maxSize = parseValues[i];
            }

            if (Physics.SphereCast(useCam.transform.position, maxSize * 0.5f, useCam.transform.forward, out RaycastHit hitInfo, 100f, -1, QueryTriggerInteraction.Ignore))
            {
                var boxGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
                var boxRB = boxGO.AddComponent<Rigidbody>();

                boxRB.interpolation = RigidbodyInterpolation.Interpolate;
                boxGO.transform.position = useCam.transform.position + (useCam.transform.forward * hitInfo.distance);
                boxGO.transform.localScale = new Vector3(parseValues[0], parseValues[1], parseValues[2]);
                boxRB.position = boxGO.transform.position;
            }
        }

        [ConCmd("rb_create_sphere", "시점이 향한 곳에 구를 생성합니다. \n- rb_create_sphere <반지름>", ExecFlag.CHEAT)]
        public static void CreateSphere(float radius)
        {
            if (!TryGetMaincam(out Camera useCam))
                return;

            if (Physics.SphereCast(useCam.transform.position, radius, useCam.transform.forward, out RaycastHit hitInfo, 100f, -1, QueryTriggerInteraction.Ignore))
            {
                var sphereGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var sphereRB = sphereGO.AddComponent<Rigidbody>();

                sphereRB.interpolation = RigidbodyInterpolation.Interpolate;
                sphereGO.transform.position = useCam.transform.position + (useCam.transform.forward * hitInfo.distance);
                sphereGO.transform.localScale *= radius * 2f;
                sphereRB.position = sphereGO.transform.position;
            }
        }

        [ConCmd("rb_destroy", "/시점이 향한 곳에 있는 강체를 파괴합니다.", ExecFlag.CHEAT)]
        public static void DestroyRigidbody()
        {
            if (!TryGetMaincam(out Camera useCam))
                return;

            if (Physics.Raycast(useCam.transform.position, useCam.transform.forward, out RaycastHit hitInfo, 100f, -1, QueryTriggerInteraction.Ignore))
            {
                if (hitInfo.transform.TryGetComponent(out Rigidbody hitRB)
                && !hitRB.isKinematic)
                {
                    Debug.Log($"{hitInfo.transform.name} 을(를) 파괴했습니다.");
                    UnityEngine.Object.Destroy(hitInfo.transform.gameObject);
                    return;
                }
            }

            Debug.Log($"<color={COLOR_ERROR}>파괴할 강체를 찾지 못했습니다.</color>");
        }

        [ConCmd("rb_setmass", "/시점이 향한 곳에 있는 강체의 질량 정보를 수정합니다.", ExecFlag.CHEAT)]
        public static void SetMass(float mass)
        {
            if (!TryGetMaincam(out Camera useCam))
                return;

            if (Physics.Raycast(useCam.transform.position, useCam.transform.forward, out RaycastHit hitInfo, 100f, -1, QueryTriggerInteraction.Ignore))
            {
                if (hitInfo.transform.TryGetComponent(out Rigidbody hitRB)
                && !hitRB.isKinematic)
                {
                    hitRB.mass = mass;
                    return;
                }
            }

            Debug.Log($"<color={COLOR_ERROR}>값을 수정할 강체를 찾지 못했습니다.</color>");
        }

        private static bool TryGetMaincam(out Camera cam)
        {
            if (_mainCam == null)
                _mainCam = Camera.main;

            cam = _mainCam;

            if (cam == null)
            {
                Debug.LogError("Main Camera 태그를 가진 카메라 오브젝트를 발견하지 못했습니다.");
                return false;
            }
            return true;
        }
    }
}