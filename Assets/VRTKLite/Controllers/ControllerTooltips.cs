using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VRTKLite.Controllers.ButtonMaps;

namespace VRTKLite.Controllers
{
    public class ControllerTooltips : MonoBehaviour
    {
        ControllerType controllerType = ControllerType.None;

        readonly Dictionary<ControllerElements, TextBox> tooltips =
            new();

        bool isLoaded = false;
        bool IsRight;

        ControllerDetection controllerDetection;

        public void SetController(string controllerName, bool isRight, GameObject controllerModel)
        {
            controllerType = ControllerTypeExtensions.ControllerTypesFromString(controllerName);
            ControllerButtonMap controllerButtonMapper = ControllerTypeExtensions.GetControllerButtonMap(
                controllerName, 
                isRight, 
                controllerModel);
            IsRight = isRight;
            GetControllerDetection(this.transform);
            if (IsRight)
            {
                controllerDetection.RightModelOff += HideToolTips;
            }
            else
            {
                controllerDetection.LeftModelOff += HideToolTips;
            }
            if (controllerButtonMapper == null)
            {
                Debug.LogError($"No button mapper for {controllerName}");
                return;
            }

            CreateTooltipFor(controllerButtonMapper, ControllerElements.Trigger, TextAlignmentOptions.Right, isRight);
            CreateTooltipFor(controllerButtonMapper, ControllerElements.GripLeft, TextAlignmentOptions.Right, isRight);
            CreateTooltipFor(controllerButtonMapper, ControllerElements.Touchpad, TextAlignmentOptions.Left, isRight);
            CreateTooltipFor(controllerButtonMapper, ControllerElements.ButtonOne, TextAlignmentOptions.Left, isRight);
            CreateTooltipFor(controllerButtonMapper, ControllerElements.ButtonTwo, TextAlignmentOptions.Right, isRight);
            CreateTooltipFor(controllerButtonMapper, ControllerElements.StartMenu, TextAlignmentOptions.Right, isRight);

            isLoaded = true;
        }

        void OnDisable()
        {
            if (IsRight)
            {
                controllerDetection.RightModelOff -= HideToolTips;
            }
            else
            {
                controllerDetection.LeftModelOff -= HideToolTips;
            }
        }

        void GetControllerDetection(Transform curGameObject)
        {
            controllerDetection = curGameObject.GetComponent<ControllerDetection>();
            if (controllerDetection == null)
            {
                GetControllerDetection(curGameObject.transform.parent);
            }
        }

        void CreateTooltipFor(ControllerButtonMap controllerButtonMapper, ControllerElements element,
            TextAlignmentOptions align, bool isRight)
        {
            Vector3 buttonPosition = controllerButtonMapper.GetElementPosition(element);

            //Index specific tooltip code:
            if (!isRight && controllerType == ControllerType.ValveIndex)
            {
                buttonPosition.x = -buttonPosition.x;
            }

            TextBox tooltip = TextBox.Create(
                "",
                align);
            tooltip.transform.SetParent(transform, false);
            tooltip.transform.localPosition = buttonPosition;

            switch (controllerType)
            {
                case ControllerType.ViveWand:
                case ControllerType.ValveIndex:
                    tooltip.transform.Rotate(Vector3.right * 80);
                    tooltip.transform.Translate(Vector3.up * 0.06f, Space.Self);
                    break;
                case ControllerType.RiftTouch:
                    tooltip.transform.Rotate(Vector3.right * 20);
                    break;
                case ControllerType.WaveVRController:
                    tooltip.transform.Rotate(Vector3.right * 50);
                    break;
            }

            if (align == TextAlignmentOptions.Left)
            {
                tooltip.transform.Translate(Vector3.right * .07f);
            }
            else if (align == TextAlignmentOptions.Right)
            {
                tooltip.transform.Translate(Vector3.left * .07f);
            }

            tooltips.Add(element, tooltip);

            tooltip.gameObject.SetActive(false);
        }

        public void UpdateText(ControllerElements element, string text)
        {
            // Ideally, we never call this method before the tooltips are loaded. However, as a stop-gap measure while
            // we are still using VRTK, the `isLoaded` check will be sufficient until we have a need for a more advanced
            // solution (such as using a coroutine).
            if (isLoaded)
            {
                try
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        tooltips[element].Text = "";
                        tooltips[element].gameObject.SetActive(false);
                        return;
                    }

                    string final = "";
                    int count = 1;
                    bool breakSpace = false;
                    char priorChar = ' ';
                    foreach (char c in text)
                    {
                        final += c;
                        if (count % 15 == 0)
                        {
                            // next break separator is getting a newline appended
                            breakSpace = true;
                        }

                        if (breakSpace &&
                            priorChar != '<' && // TMP tag escape
                            (c == ' ' || c == '/' || c == '\\')) // break separators
                        {
                            final += '\n';
                            breakSpace = false;
                        }

                        priorChar = c;
                        count++;
                    }

                    tooltips[element].Text = final;
                    ShowToolTip(tooltips[element].gameObject);
                }
                catch (KeyNotFoundException e)
                {
                }
            }
        }
        void HideToolTips()
        {
            foreach (TextBox tb in tooltips.Values)
            {
                tb.gameObject.SetActive(false);
            }
        }

        void ShowToolTip(GameObject toolTip)
        {
            if(IsRight && controllerDetection.RightModelActive || 
               !IsRight && controllerDetection.LeftModelActive)
            {
                toolTip.SetActive(true);
            }
        }
    }
}