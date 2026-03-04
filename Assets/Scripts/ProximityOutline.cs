using UnityEngine;
using System.Collections.Generic;

namespace Assets.Scripts
{
    public class ProximityOutline : MonoBehaviour
    {
        [Header("=== DETECTION DISTANCE ===")]
        [Tooltip("At what distance (meters) does the contour appear?")]
        public float detectionRadius = 3f;

        [Tooltip("Gamertag — must match the tag in Hierarchy")]
        public string playerTag = "Player";

        [Header("=== MATERIALS ===")]
        [Tooltip("For 3D objects (keys, cabinet) - material from OutlineShader")]
        public Material outlineMaterial3D;

        [Tooltip("For 2D sprites (documents) — material from URP_SpriteOutline")]
        public Material outlineMaterial2D;

        [Header("=== ANIMATION OF APPEARANCE ===")]
        [Tooltip("Smooth appearance instead of instant")]
        public bool smoothAppear = true;

        [Tooltip("Fade speed (higher = faster)")]
        public float fadeSpeed = 4f;

        private Transform playerTransform;
        private bool isSprite;
        private bool isHighlighted;
        private float currentAlpha;

        // Для 3D мешів
        private Renderer[] allRenderers;
        private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
        private Material outlineInstance3D;

        // Для спрайтів
        private SpriteRenderer spriteRenderer;
        private Material originalSpriteMaterial;
        private Material outlineInstance2D;


        void Start()
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj == null)
            {
                enabled = false;
                return;
            }
            playerTransform = playerObj.transform;

            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            isSprite = spriteRenderer != null;

            if (isSprite)
            {
                SetupSprite();
            }
            else
            {
                SetupMesh();
            }
        }
        void SetupSprite()
        {
            if (outlineMaterial2D == null)
            {
                enabled = false;
                return;
            }

            originalSpriteMaterial = spriteRenderer.sharedMaterial;

            outlineInstance2D = new Material(outlineMaterial2D);
            outlineInstance2D.name = $"SpriteOutline_{gameObject.name}";

            if (spriteRenderer.sprite != null)
            {
                outlineInstance2D.SetTexture("_MainTex", spriteRenderer.sprite.texture);
            }
        }
        void SetupMesh()
        {
            if (outlineMaterial3D == null)
            {
                enabled = false;
                return;
            }

            allRenderers = GetComponentsInChildren<Renderer>();

            if (allRenderers.Length == 0)
            {
                enabled = false;
                return;
            }

            foreach (var rend in allRenderers)
                originalMaterials[rend] = rend.sharedMaterials;

            outlineInstance3D = new Material(outlineMaterial3D);
            outlineInstance3D.name = $"Outline3D_{gameObject.name}";
        }
        void Update()
        {
            if (playerTransform == null) return;

            float distance = Vector3.Distance(transform.position, playerTransform.position);
            bool shouldHighlight = distance <= detectionRadius;

            if (smoothAppear)
            {
                float targetAlpha = shouldHighlight ? 1f : 0f;
                currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);

                UpdateMaterialAlpha(currentAlpha);

                if (currentAlpha > 0.01f && !isHighlighted) AddOutline();
                else if (currentAlpha <= 0.01f && isHighlighted) RemoveOutline();
            }
            else
            {
                if (shouldHighlight && !isHighlighted) AddOutline();
                else if (!shouldHighlight && isHighlighted) RemoveOutline();
            }
        }
        void AddOutline()
        {
            isHighlighted = true;

            if (isSprite)
            {
                spriteRenderer.material = outlineInstance2D;
            }
            else
            {
                foreach (var rend in allRenderers)
                {
                    var orig = originalMaterials[rend];
                    var newMats = new Material[orig.Length + 1];
                    for (int i = 0; i < orig.Length; i++) newMats[i] = orig[i];
                    newMats[orig.Length] = outlineInstance3D;
                    rend.materials = newMats;
                }
            }
        }
        void RemoveOutline()
        {
            isHighlighted = false;

            if (isSprite)
            {
                spriteRenderer.material = originalSpriteMaterial;
            }
            else
            {
                foreach (var rend in allRenderers)
                    rend.materials = originalMaterials[rend];
            }
        }
        void UpdateMaterialAlpha(float alpha)
        {
            Material mat = isSprite ? outlineInstance2D : outlineInstance3D;
            if (mat == null) return;

            Color col = mat.GetColor("_OutlineColor");
            mat.SetColor("_OutlineColor", new Color(col.r, col.g, col.b, alpha));
        }
        public void ForceHighlight(bool enable)
        {
            if (enable) AddOutline();
            else RemoveOutline();
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0f, 1f, 0.8f, 0.15f);
            Gizmos.DrawSphere(transform.position, detectionRadius);
            Gizmos.color = new Color(0f, 1f, 0.8f, 0.8f);
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
        void OnDestroy()
        {
            if (outlineInstance3D != null) Destroy(outlineInstance3D);
            if (outlineInstance2D != null) Destroy(outlineInstance2D);
        }
    }
}