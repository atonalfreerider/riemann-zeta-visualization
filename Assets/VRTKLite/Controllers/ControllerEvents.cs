using System;
using UnityEngine;
using UnityEngine.XR;

namespace VRTKLite.Controllers
{
    /// <summary>
    /// <para>This class unifies several different controller configurations under one class. This lets us detect
    /// controller events for a multitude of different controllers in a generic way.</para>
    /// 
    /// <para>Subclasses will use Unity's <see cref="Input"/> system to query whether the appropriate inputs should
    /// trigger their respective events in this class. We should NOT be interfacing with any specific controller
    /// hardware outside of this class's subclasses!</para>
    /// </summary>
    public class ControllerEvents : MonoBehaviour
    {
        public bool NoJoystick = false;
        
        // internal vars
        InputDevice myInputDevice;

        #region Button State

        bool isButtonOnePressed, isButtonTwoPressed;
        bool isUpButtonPressed, isRightButtonPressed, isDownButtonPressed, isLeftButtonPressed;
        bool isTriggerPressed;
        public bool IsGripPressed;

        public bool IsRightButtonPressed => isRightButtonPressed;

        #endregion

        #region Button Actions

        public event Action ButtonOnePressed;
        public event Action ButtonTwoPressed;

        public event Action UpButtonPressed, RightButtonPressed, DownButtonPressed, LeftButtonPressed;

        public event Action TriggerPressed, TriggerReleased;
        public event Action GripPressed, GripReleased;

        #endregion

        // Calibration
        [Tooltip("Percentage the trigger must be pushed in to register as a press.")]
        public float TriggerPressThreshold = 0.80f;

        [Tooltip("Percentage the grip must be pushed in to register as a squeeze.")]
        public float GripPressThreshold = 0.80f;

        const float CenterRadiusPercent = .5f;

        static bool IsTouchingCenterCircle(Vector2 touchPosition) =>
            touchPosition.sqrMagnitude < CenterRadiusPercent * CenterRadiusPercent;

        static Direction AsButtonDirection(Vector2 position) =>
            IsTouchingCenterCircle(position)
                ? Direction.None
                : position.AsDirection();

        // INIT
        public void AssignInputDevice(InputDevice inputDevice, bool isRight, GameObject controllerModel)
        {
            Debug.Log(inputDevice.name);
            string toLower = inputDevice.name.ToLowerInvariant();
            myInputDevice = inputDevice;

            if (GetComponent<ControllerTooltips>())
            {
                GetComponent<ControllerTooltips>().SetController(toLower, isRight, controllerModel);
            }
        }

        #region Check Buttons

        void CheckTrigger()
        {
            bool triggerButtonSuccess = myInputDevice.TryGetFeatureValue(
                CommonUsages.triggerButton,
                out bool isTriggerButtonPressed);
            if (triggerButtonSuccess)
            {
                if (isTriggerButtonPressed != isTriggerPressed)
                {
                    isTriggerPressed = isTriggerButtonPressed;
                    if (isTriggerButtonPressed)
                    {
                        TriggerPressed?.Invoke();
                    }
                    else
                    {
                        TriggerReleased?.Invoke();
                    }
                }
            }
            else
            {
                // fallback
                bool triggerAxisSuccess = myInputDevice.TryGetFeatureValue(
                    CommonUsages.trigger,
                    out float triggerAxis);
                if (triggerAxisSuccess)
                {
                    if (!isTriggerPressed && triggerAxis >= TriggerPressThreshold)
                    {
                        isTriggerPressed = true;
                        TriggerPressed?.Invoke();
                    }
                    else if (isTriggerPressed && triggerAxis < TriggerPressThreshold)
                    {
                        isTriggerPressed = false;
                        TriggerReleased?.Invoke();
                    }
                }
            }
        }

        void CheckPrimary()
        {
            bool primarySuccess = myInputDevice.TryGetFeatureValue(
                CommonUsages.primaryButton,
                out bool isPrimaryPressed);
            if (primarySuccess)
            {
                if (isPrimaryPressed != isButtonOnePressed)
                {
                    isButtonOnePressed = isPrimaryPressed;
                    if (isPrimaryPressed)
                    {
                        ButtonOnePressed?.Invoke();
                    }
                }
            }
            else
            {
                // fallback to touchpad
                bool centerStick = myInputDevice.TryGetFeatureValue(
                    CommonUsages.primary2DAxis,
                    out Vector2 primaryAxis);

                if (centerStick)
                {
                    bool downState = AsButtonDirection(primaryAxis) == Direction.None &&
                                     HasPrimaryAxisClickAndIsPressed();
                    if (downState != isButtonOnePressed)
                    {
                        isButtonOnePressed = downState;
                        if (downState)
                        {
                            ButtonOnePressed?.Invoke();
                        }
                    }
                }
            }
        }

        void CheckSecondary()
        {
            bool secondarySuccess = myInputDevice.TryGetFeatureValue(
                CommonUsages.secondaryButton,
                out bool isSecondaryPressed);
            if (secondarySuccess)
            {
                if (isSecondaryPressed != isButtonTwoPressed)
                {
                    isButtonTwoPressed = isSecondaryPressed;
                    if (isSecondaryPressed)
                    {
                        ButtonTwoPressed?.Invoke();
                    }
                }
            }
            else
            {
                // fallback -> use menu as secondary button
                bool menuSuccess = myInputDevice.TryGetFeatureValue(
                    CommonUsages.menuButton,
                    out bool isMenuPressed);
                if (menuSuccess)
                {
                    if (isMenuPressed != isButtonTwoPressed)
                    {
                        isButtonTwoPressed = isMenuPressed;
                        if (isMenuPressed)
                        {
                            ButtonTwoPressed?.Invoke();
                        }
                    }
                }
            }
        }

        void CheckGrip()
        {
            bool gripSuccess = myInputDevice.TryGetFeatureValue(
                CommonUsages.grip,
                out float gripAxis);
            if (gripSuccess)
            {
                if (!IsGripPressed && gripAxis >= GripPressThreshold)
                {
                    IsGripPressed = true;
                    GripPressed?.Invoke();
                }
                else if (IsGripPressed && gripAxis < GripPressThreshold)
                {
                    IsGripPressed = false;
                    GripReleased?.Invoke();
                }
            }
        }

        #endregion

        bool HasPrimaryAxisClickAndIsPressed()
        {
            bool padSuccess = myInputDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool isPadPressed);

            // If there is no joystick or touchpad click/depression feature, return false. 
            if (!padSuccess) return false;

            // Otherwise, return the current state of the touchpach or joystick click/depression.
            // if it IS pressed down, the calling function will return. In this instance, 
            // the touchpad or joystick is pressed down, so we don't want to check the axis values.
            // if it is NOT pressed down, the calling function will continue with checking the axis values
            return isPadPressed;
        }

        bool ResetDirectionButtonsIfNotPressed()
        {
            bool padSuccess = myInputDevice.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool isPadPressed);

            //INFO:
            //If the controller has a joystick or touchpad feature
            //If said joystick or touchpad is NOT pressed down
            //Then reset all direction button values and return true.
            //We return true because the current direction is nothing, meaning we don't
            //need to check the axis values (where this function is called)
            if (padSuccess && !isPadPressed)
            {
                ResetDirectionButtons();
                return true;
            }

            //We return false when there isn't a joystick or touchpad feature
            //This allows the calling function to continue with checking the stick directional values
            return false;
        }

        void ResetDirectionButtons()
        {
            isUpButtonPressed = false;
            isRightButtonPressed = false;
            isDownButtonPressed = false;
            isLeftButtonPressed = false;
        }

        void CheckAxis()
        {
            if (NoJoystick && ResetDirectionButtonsIfNotPressed()) return; // just do a button reset

            bool primaryAxisSuccess = myInputDevice.TryGetFeatureValue(
                CommonUsages.primary2DAxis,
                out Vector2 primaryAxis);

            if (!primaryAxisSuccess) return; // device does not contain an axis

            // at this point a directional button may be getting pressed for the first time
            // if so, that directional callback will be invoked and the button will be in a "pressed" state
            Direction direction = AsButtonDirection(primaryAxis);
            switch (direction)
            {
                case Direction.None:
                    // case handled by primary button
                    ResetDirectionButtons();
                    break;
                case Direction.Up:
                    if (!isUpButtonPressed)
                    {
                        isUpButtonPressed = true;
                        UpButtonPressed?.Invoke();
                    }

                    break;
                case Direction.Right:
                    if (!isRightButtonPressed)
                    {
                        isRightButtonPressed = true;
                        RightButtonPressed?.Invoke();
                    }

                    break;
                case Direction.Down:
                    if (!isDownButtonPressed)
                    {
                        isDownButtonPressed = true;
                        DownButtonPressed?.Invoke();
                    }

                    break;
                case Direction.Left:
                    if (!isLeftButtonPressed)
                    {
                        isLeftButtonPressed = true;
                        LeftButtonPressed?.Invoke();
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void Update()
        {
            CheckTrigger();
            CheckPrimary();
            CheckSecondary();
            CheckGrip();
            CheckAxis();
        }
    }
}