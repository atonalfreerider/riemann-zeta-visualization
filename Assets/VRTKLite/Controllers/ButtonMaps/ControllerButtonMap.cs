using UnityEngine;

namespace VRTKLite.Controllers.ButtonMaps
{
    public abstract class ControllerButtonMap
    {
        protected GameObject controllerModel;
        
        /// <summary>
        /// Gets the position of a transform that corresponds to a button location. The base version uses names of
        /// buttons to recursively find a button. It will get overriden if individual buttons don't exist. 
        /// </summary>
        public virtual Vector3 GetElementPosition(ControllerElements element)
        {
            string elementPath = ElementPath(element);
            if (controllerModel == null || string.IsNullOrEmpty(elementPath)) return Vector3.zero;

            Transform child = Recurse(controllerModel.transform, elementPath); 

            return child == null ? Vector3.zero : child.position;
        }

        static Transform Recurse(Transform parent, string elementPath)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLowerInvariant().Contains(elementPath.ToLowerInvariant()))
                {
                    return child;
                }

                Transform grandchild = Recurse(child, elementPath);
                if (grandchild != null)
                {
                    return grandchild;
                }
            }

            return null;
        }
        
        protected virtual string ElementPath(ControllerElements element)
        {
            return "";
        }
    }
}