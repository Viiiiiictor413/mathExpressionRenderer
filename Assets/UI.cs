using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;
using System.Linq;
using System;

[RequireComponent(typeof(Collider2D))]
public class UI : MonoBehaviour {
	public MathExpressionRenderer mathRenderer;
	public Camera UICam;
	public RectTransform inputGameObject;
	public GameObject shorthandGameObject;
	public GameObject shorthandContentGameObject;
	public Scrollbar shorthandScrollBar;
	public GameObject operatorGameObject;
	public GameObject operatorContentGameObject;
	public Scrollbar operatorScrollBar;
	
	public float smoothTime;
	public SmoothInElement[] elements;
	
	private bool firstFrame = true;
	
	private const string DISPLAY_ARG_NAMES = "xyzabcdefghijklmnopqrstuvw";
	
    void Awake() {
		foreach (CharacterShorthand shorthand in mathRenderer.shorthands.Concat(mathRenderer.htmlTagShorthands).ToArray()) {
			GameObject instance = Instantiate(shorthandGameObject, shorthandContentGameObject.transform);
			instance.transform.Find("Key").GetComponent<TMPro.TextMeshProUGUI>().text = shorthand.key;
			instance.transform.Find("Val").GetComponent<TMPro.TextMeshProUGUI>().text = shorthand.val;
			instance.GetComponent<Button>().onClick.AddListener(() => {this.AddTextToExpression(shorthand.key);});
			instance.SetActive(true);
		}
		
		foreach (Operator op in mathRenderer.operators) {
			GameObject instance = Instantiate(operatorGameObject, operatorContentGameObject.transform);
			
			string displayText = op.name;
			string startText = op.name;
			string midText = "";
			string endText = "";
			int argNameId = 0;
			foreach (OperatorLinkedGroup linkedGroup in op.linkedGroups) {
				if (linkedGroup.type == operatorLinkedGroupTypes.explicitString) continue;
				
				if (linkedGroup.type == operatorLinkedGroupTypes.closingString) {
					displayText += "<color=blue>" + DISPLAY_ARG_NAMES[argNameId] + "</color>" + linkedGroup.stringVal;
					if (midText == "") midText = linkedGroup.stringVal;
					else endText += linkedGroup.stringVal;
				} else {
					if (linkedGroup.type == operatorLinkedGroupTypes.previousNumber) {
						displayText = "<color=blue>{" + DISPLAY_ARG_NAMES[argNameId] + "}</color>" + displayText;
						if (startText != "") {
							midText = startText + midText;
							startText = "";
						}
					} else if (linkedGroup.type == operatorLinkedGroupTypes.nextNumber) {
						displayText += "<color=blue>{" + DISPLAY_ARG_NAMES[argNameId] + "}</color>";
					} else if (linkedGroup.type == operatorLinkedGroupTypes.previousGroups) {
						displayText = "<color=blue>" + DISPLAY_ARG_NAMES[argNameId] + "</color>" + displayText;
						if (startText != "") {
							midText = startText + midText;
							startText = "";
						}
					} else {
						displayText += "<color=blue>" + DISPLAY_ARG_NAMES[argNameId] + "</color>";
					}
				}
				argNameId++;
				if (argNameId >= DISPLAY_ARG_NAMES.Length) argNameId = 0;
			}
			
			instance.transform.Find("Text").GetComponent<TMPro.TextMeshProUGUI>().text = displayText;
			instance.GetComponent<Button>().onClick.AddListener(() => {this.AddTextToExpression(startText, midText, endText);});
			instance.SetActive(true);
		}
		
		foreach (SmoothInElement element in elements) {
			element.shownPos = element.transform.position;
			element.transform.position = element.shownPos + element.hiddenOffset;
		}
    }
	
	void Start() {
		shorthandScrollBar.value = 1f;
		operatorScrollBar.value = 1f;
		
		mathRenderer.expressionInput.text = mathRenderer.expression;
	}

    void Update() {
		// Vector3 normalizedMousePos = Vector3.Scale(Input.mousePosition, new Vector3(1f / Screen.width, 1f / Screen.height, 0));
		// bool show = (EventSystem.current.currentSelectedGameObject != null || this.GetComponent<Collider2D>().OverlapPoint(Camera.main.ScreenToWorldPoint(Input.mousePosition))) && Mathf.Abs(normalizedMousePos.x - 0.5f) <= 0.5f && Mathf.Abs(normalizedMousePos.y - 0.5f) <= 0.5f;
		
		foreach (SmoothInElement element in elements) {
			bool show = (element.toggle == null || element.toggle.isOn);
			Vector2 targetPos = element.shownPos + (show ? Vector2.zero : element.hiddenOffset) + Vector2.Scale(element.screenSizeOffset, new Vector2(UICam.orthographicSize * UICam.aspect, UICam.orthographicSize));
			element.transform.position = Vector2.SmoothDamp(element.transform.position, targetPos, ref element.vel, (firstFrame ? 0f : smoothTime));
		}
		
		// if (!show) {
			// mathRenderer.expressionInput.caretColor = Color.clear;
		// } else {
			// if (EventSystem.current.currentSelectedGameObject != null) {
				// mathRenderer.expressionInput.caretColor = new Color32(50, 50, 50, 255);
				// mathRenderer.expressionInput.Select();
				// mathRenderer.expressionInput.ActivateInputField();
			// }
		// }
		
		inputGameObject.sizeDelta = new Vector2(UICam.orthographicSize * UICam.aspect * 2f - 120f, inputGameObject.sizeDelta.y);
		
		firstFrame = false;
    }
	
	public void AddTextToExpression(string startText, string midText = "", string endText = "") {
		mathRenderer.expressionInput.text = mathRenderer.expressionInput.text.Insert(Mathf.Max(mathRenderer.expressionInput.selectionAnchorPosition, mathRenderer.expressionInput.selectionFocusPosition), midText + endText)
																			 .Insert(Mathf.Min(mathRenderer.expressionInput.selectionAnchorPosition, mathRenderer.expressionInput.selectionFocusPosition), startText);
		bool hasSelection = (mathRenderer.expressionInput.selectionAnchorPosition != mathRenderer.expressionInput.selectionFocusPosition); 
		mathRenderer.expressionInput.stringPosition = Mathf.Max(mathRenderer.expressionInput.selectionAnchorPosition, mathRenderer.expressionInput.selectionFocusPosition) + startText.Length + (hasSelection || startText == "" ? midText.Length : 0);
		mathRenderer.expressionInput.Select();
        mathRenderer.expressionInput.ActivateInputField();
	}
}

[System.Serializable]
public class SmoothInElement {
	public Transform transform;
	public Vector2 hiddenOffset;
	public Vector2 screenSizeOffset;
	public Toggle toggle;
	
	[HideInInspector] public Vector2 shownPos;
	[HideInInspector] public Vector2 vel;
}