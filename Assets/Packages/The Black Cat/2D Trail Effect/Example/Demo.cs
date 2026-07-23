using UnityEngine;
using UnityEngine.InputSystem;

namespace TheBlackCat.TrailEffect2D
{
    [RequireComponent(typeof(BoxCollider2D))]
    public class Demo : MonoBehaviour
    {
        [SerializeField] private bool rotate;
        [SerializeField] private float rotateSpeed;

        private Vector3 screenPoint;
        private Vector3 offset;

        void OnMouseDown()
        {
            offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
            TrailManager.Instance.StartTrail(gameObject);
        }

        void OnMouseDrag()
        {
            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
            transform.position = curPosition;

            if (rotate)
            {
                transform.Rotate(transform.forward, Time.deltaTime * rotateSpeed);
            }
        }

        private void Update()
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
            TrailManager.Instance.StopTrail(gameObject);
                Debug.Log("왼쪽 마우스 버튼 클릭!");
            }
            
        }
    }
}
