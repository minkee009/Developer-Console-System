using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using DeveloperConsole;

public static class RigidBodyCMD
{
    public const string COLOR_ERROR = "#F78181";
    public const string COLOR_INFO = "#F7BE81";
    public const string COLOR_VALUE = "#F5DA81";

    [ConCmd("rb_gravity","강체의 전역 중력값")]
    public static float GlobalGravity = 25f;

    public static bool GlobalInterPolate = false;

    [ConCmd("rb_interpolate","전역 보간설정",ExecFlag.NONE, "GlobalInterPolate")]
    public static void SetGlobalInterpolation(bool value)
    {
        GlobalInterPolate = value;
    }

    private static Camera _mainCam;

    private static int _playerIgnoreMask = ~(1 << 6);

    [ConCmd("rb_create_box", "시점이 향한 곳에 상자를 생성합니다. \n- rb_create_box <x 스케일>, <y 스케일>, <z 스케일>", ExecFlag.CHEAT)]
    public static void CreateBox(string optArg)
    {
        var args = optArg.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (args.Length != 3)
        {
            Debug.LogError("박스 생성 오류, 인자를 확인해주세요");
            return;
        }

        if (TryGetMaincam(out Camera useCam))
            return;

        var maxSize = 0f;
        var parseValues = new float[3];

        for (int i = 0; i < args.Length; i++)
        {
            parseValues[i] = float.Parse(args[i]);
            if (maxSize < parseValues[i])
                maxSize = parseValues[i];
        }

        if (Physics.SphereCast(useCam.transform.position, maxSize * 0.5f, useCam.transform.forward, out RaycastHit hitInfo, 100f, _playerIgnoreMask, QueryTriggerInteraction.Ignore))
        {
            var boxGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var boxRB = boxGO.AddComponent<Rigidbody>();
            var boxMeshRenderer = boxGO.GetComponent<MeshRenderer>();
            var boxMaterials = boxMeshRenderer.materials;
            var boxMaterial = Resources.Load<Material>("Materials/URP_Default");

            boxMaterials[0] = boxMaterial;
            boxMeshRenderer.materials = boxMaterials;

            boxRB.interpolation = RigidbodyInterpolation.Interpolate;
            boxGO.transform.position = useCam.transform.position + (useCam.transform.forward * hitInfo.distance);
            boxGO.transform.localScale = new Vector3(parseValues[0], parseValues[1], parseValues[2]);
            boxRB.position = boxGO.transform.position;
        }
    }

    [ConCmd("rb_destroy", "/시점이 향한 곳에 있는 강체를 파괴합니다.", ExecFlag.CHEAT)]
    public static void RemoveBox()
    {
        if (TryGetMaincam(out Camera useCam))
            return;

        if (Physics.Raycast(useCam.transform.position, useCam.transform.forward, out RaycastHit hitInfo, 100f, _playerIgnoreMask, QueryTriggerInteraction.Ignore))
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
        if (TryGetMaincam(out Camera useCam))
            return;
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