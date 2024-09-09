using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DeveloperConsole;

public class DebugConsoleUI : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        DevConsole.AddAllCommandInAssembly();
        if (DevConsole.FindCommand("rb_gravity") is ConsoleCommand a)
            Debug.Log(a.Name);
    }


    private int _gravityIdx = 0;
    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.G))
        {
            _gravityIdx++;
            _gravityIdx %= 3;

            switch(_gravityIdx)
            {
                case 0:
                    DevConsole.FindCommand("rb_gravity").FloatAction.Invoke(25f);
                    break;
                case 1:
                    DevConsole.FindCommand("rb_gravity").FloatAction.Invoke(1f);
                    break;
                case 2:
                    DevConsole.FindCommand("rb_gravity").FloatAction.Invoke(-5f);
                    break;
            }

            Debug.Log(RigidBodyCMD.GlobalGravity);
        }

        if(Input.GetKeyDown(KeyCode.R))
        {
            var cmd_rb_interpolate = DevConsole.FindCommand("rb_interpolate");

            var currentInterpolateOpt = bool.Parse(cmd_rb_interpolate.TrackedValue.Invoke());

            cmd_rb_interpolate.Invoke(!currentInterpolateOpt);

            Debug.Log($"초기값 - {cmd_rb_interpolate.InitValue} : 현재 - {cmd_rb_interpolate.TrackedValue.Invoke()}" );
        }
    }
}
