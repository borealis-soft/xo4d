using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XO3D
{
	public enum CellState : byte
	{
		Empty, X, O, Count
	}
	public class Cell : MonoBehaviour
	{
		[HideInInspector] public Vector3Int index;
		[HideInInspector] public CellState state;
		[HideInInspector] public Material markMaterial;
		private bool isHidden;
		private Coroutine hideCoroutine, showCoroutine;
		private Material material;
		private float opacity = 1;
		private new Collider collider;
		private void Awake()
		{
			material = GetComponent<MeshRenderer>().material;
			collider = GetComponent<Collider>();
		}
		private void OnMouseUpAsButton()
		{
			Game.Instance.MakeMove(this);
		}
		private void OnMouseEnter()
		{
			Game.Instance.Highlight(this);
		}
		private void OnMouseExit()
		{
			Game.Instance.Unhighlight();
		}
		public void Hide()
		{
			if (isHidden) return;
			isHidden = true;

			if (showCoroutine != null) StopCoroutine(showCoroutine);
			hideCoroutine = StartCoroutine(HideCoroutine());
			collider.enabled = false;
		}
		public void Show()
		{
			if (!isHidden) return;
			isHidden = false;

			if (hideCoroutine != null) StopCoroutine(hideCoroutine);
			showCoroutine = StartCoroutine(ShowCoroutine());
			collider.enabled = true;
		}
		public void SetMarkMaterial(Material material)
		{
			markMaterial = material;
			UpdateMaterial(markMaterial);
		}

		private IEnumerator HideCoroutine()
		{
			SetMaterialTransparent();
			while (opacity > 0)
			{
				opacity = Mathf.Max(opacity - Time.deltaTime * 5, 0);
				UpdateMaterial();
				yield return null;
			}
		}
		private IEnumerator ShowCoroutine()
		{
			while (opacity < 1)
			{
				opacity = Mathf.Min(opacity + Time.deltaTime * 5, 1);
				UpdateMaterial();
				yield return null;
			}
			SetMaterialOpaque();
		}
		private void UpdateMaterial()
		{
			UpdateMaterial(material);
			if (markMaterial != null)
			{
				UpdateMaterial(markMaterial);
			}
		}
		private void SetMaterialOpaque()
		{
			SetMaterialOpaque(material);
		}
		private void SetMaterialTransparent()
		{
			SetMaterialTransparent(material);
		}

		private void UpdateMaterial(Material material)
		{
			Color color = material.GetColor("_Color");
			color.a = opacity;
			material.SetColor("_Color", color);
		}
		//
		// These functions are stolen from StandardShaderGUI.cs in built-in shaders
		// 
		private void SetMaterialOpaque(Material material)
		{
			material.SetOverrideTag("RenderType", "");
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
			material.SetInt("_ZWrite", 1);
			material.DisableKeyword("_ALPHATEST_ON");
			material.DisableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = -1;
		}
		private void SetMaterialTransparent(Material material)
		{
			material.SetOverrideTag("RenderType", "Transparent");
			material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			material.SetInt("_ZWrite", 0);
			material.DisableKeyword("_ALPHATEST_ON");
			material.EnableKeyword("_ALPHABLEND_ON");
			material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
			material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
		}
	}
}