using UnityEngine;

namespace Assets.Scripts
{
    public class WeaponBobbing : MonoBehaviour
    {
        [Header("Bobbing Settings")]
        public float walkingBobbingSpeed = 14f;
        public float bobbingAmount = 0.05f;
        public float smooth = 10f;

        private float defaultPosY = 0;
        private float timer = 0;

        void Start()
        {
            defaultPosY = transform.localPosition.y;
        }

        void Update()
        {
            float waveslice = 0.0f;
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            Vector3 cSharpConversion = transform.localPosition; 

            if (Mathf.Abs(horizontal) == 0 && Mathf.Abs(vertical) == 0)
            {
                timer = 0.0f;
            }
            else
            {
                waveslice = Mathf.Sin(timer);
                timer = timer + walkingBobbingSpeed * Time.deltaTime;
                if (timer > Mathf.PI * 2)
                {
                    timer = timer - (Mathf.PI * 2);
                }
            }

            if (waveslice != 0)
            {
                float translateChange = waveslice * bobbingAmount;
                float totalAxes = Mathf.Abs(horizontal) + Mathf.Abs(vertical);
                totalAxes = Mathf.Clamp(totalAxes, 0.0f, 1.0f);
                translateChange = totalAxes * translateChange;
                cSharpConversion.y = Mathf.Lerp(cSharpConversion.y, defaultPosY + translateChange, Time.deltaTime * smooth);
            }
            else
            {
                cSharpConversion.y = Mathf.Lerp(cSharpConversion.y, defaultPosY, Time.deltaTime * smooth);
            }

            transform.localPosition = cSharpConversion;
        }
    }
}