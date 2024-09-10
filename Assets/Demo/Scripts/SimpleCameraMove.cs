using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.Windows;
#endif

namespace SPTr.Demo
{
    public class SimpleCameraMove : MonoBehaviour
    {
        [Range(0.1f, 15f)]
        public float rotateSensitivity = 5f;

        private Vector3 _velocity;
        private float _rotX, _rotY;
        private bool _enableControl = false;

        private const float INPUTSYSTEM_SENSITIVITY = 0.02f;

        // Update is called once per frame
        void Update()
        {
            CheckRightButtonClick();
            RotateCam();
            MoveCam();
        }

        /// <summary>
        /// 카메라 회전
        /// </summary>
        public void RotateCam()
        {
            if (_enableControl)
            {
                Cursor.lockState = CursorLockMode.Locked;
#if ENABLE_INPUT_SYSTEM

                var mouse = Mouse.current;

                var trueSensitivity = INPUTSYSTEM_SENSITIVITY * rotateSensitivity;

                _rotY += mouse.delta.x.value * trueSensitivity;
                _rotX -= mouse.delta.y.value * trueSensitivity;
#else
                _rotY += Input.GetAxis("Mouse X") * rotateSensitivity;
                _rotX -= Input.GetAxis("Mouse Y") * rotateSensitivity;
#endif
                _rotX = Mathf.Clamp(_rotX, -89f, 89f);
                transform.rotation = Quaternion.Euler(_rotX, _rotY, 0);
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
            }

        }

        public void MoveCam()
        {
            Vector3 targetVelocity = Vector3.zero;
            float speedFactor;
            if (_enableControl)
            {
                float inputX, inputY, inputZ;
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                inputX = keyboard.dKey.value - keyboard.aKey.value;
                inputY = keyboard.eKey.value - keyboard.qKey.value;
                inputZ = keyboard.wKey.value - keyboard.sKey.value;

                speedFactor = keyboard.leftShiftKey.isPressed ? 2.0f : 1.0f;
#else
                inputX = (Input.GetKey(KeyCode.A) ? -1 : 0) + (Input.GetKey(KeyCode.D) ? 1 : 0);
                inputY = (Input.GetKey(KeyCode.Q) ? -1 : 0) + (Input.GetKey(KeyCode.E) ? 1 : 0);
                inputZ = (Input.GetKey(KeyCode.S) ? -1 : 0) + (Input.GetKey(KeyCode.W) ? 1 : 0);

                speedFactor = Input.GetKey(KeyCode.LeftShift) ? 2.0f : 1.0f;
#endif
                targetVelocity = new Vector3(inputX, inputY, inputZ).normalized * speedFactor * CamControlCMD.cameraMoveSpeed;
                targetVelocity = transform.TransformVector(targetVelocity);
            }
            _velocity = Vector3.Lerp(_velocity, targetVelocity, CamControlCMD.cameraMoveSharpness * Time.deltaTime);

            transform.position += _velocity * Time.deltaTime;
        }

        /// <summary>
        /// 마우스 우클릭 확인
        /// </summary>
        public void CheckRightButtonClick()
        {
#if ENABLE_INPUT_SYSTEM
            _enableControl = Mouse.current.rightButton.isPressed;

#else
            _enableControl = Input.GetMouseButton(1);
#endif
        }
    }
}


