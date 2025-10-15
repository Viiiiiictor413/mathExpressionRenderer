using TMPro;
using System;
using UnityEditor;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Collections;
using System.Globalization;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using System.Collections.Generic;

[ExecuteInEditMode]
public class MathExpressionRenderer : MonoBehaviour {
	public Camera cam;
	public RectTransform canvas;
	public TMP_InputField expressionInput;
	public GameObject groupGameObject;
	public GameObject groupsGameObject;
	
	[Space(20)]
	[Range(0.01f, 1.0f)] public float scrollStrength;
	public float moveSpeed;
	public Sprite missingImage;

	[Space(20)]
	[TextArea(4, 10)] public string expression;
	public bool debug;
	public bool parse;
	public Group groups;
	
	[Space(20)]
	public Operator[] operators;
	public string italicize;
	public string canAlwaysPrecedeNumber;
	public string canPrecedeNumberInNextGroups;
	public string canFollowNumber;
	public CharacterShorthand[] shorthands;
	public CharacterShorthand[] htmlTagShorthands;
	
	[HideInInspector] public static float textLineHeight;
	[HideInInspector] public static float textCharWidth;
	private List<TagList> htmlTags;
	private List<string> currentTags;
	private Vector2 renderOffsetSum;
	private Vector3? prevMousePos = null;
	private Vector3 prevGroupsPos;
	private string prevExpressionInputText;

    void Start() {
		prevExpressionInputText = expressionInput.text;
	}
	
	void Update() {	
		if (expressionInput.text != prevExpressionInputText) {
			expression = expressionInput.text;
			parse = true;
			prevExpressionInputText = expressionInput.text;
		}
		
		if (Input.GetKeyDown(KeyCode.N) && EventSystem.current.currentSelectedGameObject == null) {
			debug = !debug;
			parse = true;
		}
		
		if (parse) {
			parse = false;
			ParseExpression();
		}
		
		if (Application.isPlaying
		&& Input.mousePosition.x / (float)Screen.width >= 0f && Input.mousePosition.x / (float)Screen.width <= 1f
		&& Input.mousePosition.y / (float)Screen.height >= 0f && Input.mousePosition.y / (float)Screen.height <= 1f) {
			if (EventSystem.current.currentSelectedGameObject == null) groupsGameObject.transform.position += moveSpeed * new Vector3(-Input.GetAxis("Horizontal"), -Input.GetAxis("Vertical"), 0f);
			
			if (!IsPointerOverUIElement()) {
				float prevGroupsScale = groupsGameObject.transform.localScale.x;
				groupsGameObject.transform.localScale *= Mathf.Pow(1f + scrollStrength, Input.mouseScrollDelta.y);
				Vector3 deltaPos = (groupsGameObject.transform.localScale.x / prevGroupsScale - 1f) * (groupsGameObject.transform.position - Vector3.Scale(Camera.main.ScreenToWorldPoint(Input.mousePosition), new Vector3(1f, 1f, 0f)));
				groupsGameObject.transform.position += deltaPos;
				prevGroupsPos += deltaPos;
				
				if (Input.GetMouseButton(0)) {
					if (Input.GetMouseButtonDown(0)) {
						prevMousePos = Vector3.Scale(Camera.main.ScreenToWorldPoint(Input.mousePosition), new Vector3(1f, 1f, 0f));
						prevGroupsPos = groupsGameObject.transform.position;
					}
					if (prevMousePos != null) groupsGameObject.transform.position = Vector3.Scale(Camera.main.ScreenToWorldPoint(Input.mousePosition), new Vector3(1f, 1f, 0f)) - (Vector3)prevMousePos + prevGroupsPos;
				}
				
				if (Input.GetKeyDown(KeyCode.R) && EventSystem.current.currentSelectedGameObject == null) {
					groupsGameObject.transform.position = Vector3.zero;
					groupsGameObject.transform.localScale = Vector3.one;
					prevMousePos = null;
				}
			} else prevMousePos = null;
		} else prevMousePos = null;
    }
	
    private bool IsPointerOverUIElement() {
		List<RaycastResult> raycastResults = GetEventSystemRaycastResults();
        for (int i = 0; i < raycastResults.Count; i++) {
            if (raycastResults[i].gameObject.layer == 5) return true;
        }
        return false;
    }
	
	public static List<RaycastResult> GetEventSystemRaycastResults() {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;
        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        return raycastResults;
    }
	
	public void ParseExpression() {
		// Clear console
		var logEntries = Type.GetType("UnityEditor.LogEntries, UnityEditor");
        var clearMethod = logEntries?.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
        clearMethod?.Invoke(null, null);
		
		float duration = Time.realtimeSinceStartup;
		bool success = true;
		string initialExpression = expression;
		TMPro.TextMeshProUGUI tmp = groupGameObject.GetComponent<TMPro.TextMeshProUGUI>();
		
		// Store HTML tags and parse shorthands
		htmlTags = new List<TagList>();
		int tagLength = 0;
		int textDepth = 0;
		int shorthandAndTagPasses = 0;
		for (int c = 0; c < expression.Length; c++) {
			if (htmlTags.Count <= c) htmlTags.Add(new TagList());
			// Tags
			if (tagLength == 0) {
				if (expression[c] == '<') tagLength = 1;
			} else {
				tagLength++;
				if (expression[c] == '>') {
					string tag = expression.Substring(c - (tagLength - 1), tagLength);
					if (tag == "<text>") {
						textDepth++;
						// Insert ZWSP to prevent junction with neighbouring groups
						expression = expression.Insert(c + 1, "​");
					} else if (tag == "</text>") {
						textDepth = Mathf.Max(textDepth - 1, 0);
						// Insert ZWSP to prevent junction with neighbouring groups
						expression = expression.Insert(c + 1, "​");
					} else if (tag != "<br>") {
						// Check if this is a functionnal tag
						groupGameObject.SetActive(true);
						tmp.text = tag;
						tmp.ForceMeshUpdate(true);
						if (tmp.GetParsedText() != "") tag = "";
						groupGameObject.SetActive(false);
					}
					if (tag != "") {
						htmlTags[c - (tagLength - 1)].tags.Add(tag);
						expression = expression.Substring(0, c - (tagLength - 1)) + (c == expression.Length - 1 ? "" : expression.Substring(c + 1));
						for (int i = 0; i < tagLength - 1; i++) htmlTags.RemoveAt(c - (tagLength - 1) + 1);
						c-= tagLength;
					}
					tagLength = 0;
				}
			}
			
			// Shorthands
			if (c >= 0) {
				for (int tagShorthands = 0; tagShorthands < 2; tagShorthands++) {
					if (!(tagShorthands == 0 && textDepth != 0)) {
						CharacterShorthand longestMatchingShorthand = null;
						foreach (CharacterShorthand shorthand in (tagShorthands == 0 ? shorthands : htmlTagShorthands)) {
							if ((longestMatchingShorthand == null || shorthand.key.Length > longestMatchingShorthand.key.Length) && expression.Substring(c).StartsWith(shorthand.key)) {
								longestMatchingShorthand = shorthand;
							}
						}
						if (longestMatchingShorthand != null) {
							expression = expression.Substring(0, c) + longestMatchingShorthand.val + expression.Substring(c + longestMatchingShorthand.key.Length, expression.Length - c - longestMatchingShorthand.key.Length);
							c--;
							tagLength = 0;
							break;
						}
					}
				}
			}
			
			shorthandAndTagPasses++;
			if (shorthandAndTagPasses > 1000) {
				Debug.LogError("Couldn't parse all shorthands and tags !");
				success = false;
				break;
			}
		}
		
		// Compute text dimensions
		groupGameObject.SetActive(true);
		tmp.text = "0";
		tmp.ForceMeshUpdate(true);
		textLineHeight = tmp.GetRenderedValues(false).y;
		textCharWidth = tmp.GetRenderedValues(false).x;
		float textBaseline = tmp.textInfo.characterInfo[0].bottomLeft.y;
		float textBottomGap = Mathf.Abs(tmp.textInfo.lineInfo[0].baseline - tmp.textInfo.lineInfo[0].descender);
		groupGameObject.SetActive(false);
		
		// Compute groups and their dependencies
		groups = new Group("\\");
		List<int> path = new List<int>();
		string currentGroup = "";
		textDepth = 0;
		for (int charId = 0; charId < expression.Length; charId++) {
			char c = expression[charId];
			currentGroup += c;
			bool madeNewGroup = false;
			
			if (htmlTags[charId].tags != null) {
				foreach (string tag in htmlTags[charId].tags) {
					if (tag == "<text>") textDepth++;
					else if (tag == "</text>") textDepth = Mathf.Max(textDepth - 1, 0);
				}
			}
			
			if (!madeNewGroup && textDepth == 0) {
				// If last characters of current group close an open group, create new groups accordingly
				// Check all open groups, last to first
				int canCloseGroup = -1;
				int amountToIncrement = 0;
				for (int i = path.Count - 1; i >= 0; i--) {
					Group closingGroup = GetGroupAt(path.GetRange(0, i + 1));
					if (closingGroup.op == null) continue;
					amountToIncrement = 0;
				
					// Check from currently open linked group, to see if it can be closed
					for (int j = closingGroup.openLinkedGroup; j < closingGroup.op.linkedGroups.Length; j++) {
						amountToIncrement++;
						OperatorLinkedGroup matchingOpOpenLinkedGroup = closingGroup.op.linkedGroups[j];
						// Special character that always closes the current open group
						if (c == '@') {
							canCloseGroup = i;
							if (currentGroup.Length > 1) {
								GetGroupAt(path).linkedGroups.Add(new Group(currentGroup.Substring(0, currentGroup.Length - 1), charId - 1));
							}
						} else {
							if (matchingOpOpenLinkedGroup.type == operatorLinkedGroupTypes.closingString) {
								if (currentGroup.EndsWith(matchingOpOpenLinkedGroup.stringVal, false, null)) {
									// If same string closes and opens group (for instance absolute value's '|'), only close if on the same path level
									if (matchingOpOpenLinkedGroup.stringVal != closingGroup.op.name || i == path.Count - 2) {
										canCloseGroup = i;
										if (matchingOpOpenLinkedGroup.stringVal.Length < currentGroup.Length) {
											GetGroupAt(path).linkedGroups.Add(new Group(currentGroup.Substring(0, currentGroup.Length - matchingOpOpenLinkedGroup.stringVal.Length), charId - matchingOpOpenLinkedGroup.stringVal.Length));
										}
									}
								}
							} 
						}
						if (canCloseGroup != -1) break;
					}
					if (canCloseGroup != -1) break;
				}
				if (canCloseGroup == -1 && path.Count > 0) {
					amountToIncrement = 1;
					Group closingGroup = GetGroupAt(path.GetRange(0, path.Count - 1));
					if (closingGroup.op.linkedGroups[closingGroup.openLinkedGroup].type == operatorLinkedGroupTypes.nextNumber) {
						if (!GetGroupAt(path).startsWithOp && charId != expression.Length - 1) {
							Operator longestMatchingOp = null;
							foreach (Operator op in operators) {
								if ((longestMatchingOp == null || op.name.Length > longestMatchingOp.name.Length) && expression.Substring(charId).StartsWith(op.name)) {
									longestMatchingOp = op;
								}
							}
							if (longestMatchingOp == null) {
								int followingChars = 0;
								while (followingChars < currentGroup.Length && canFollowNumber.Contains(expression[charId + 1 - followingChars])) {
									followingChars++;
								}
								bool startsWithSign = canPrecedeNumberInNextGroups.Contains(currentGroup[0]) || canAlwaysPrecedeNumber.Contains(currentGroup[0]);
								if (startsWithSign && currentGroup.Length == 1) {
									longestMatchingOp = null;
									foreach (Operator op in operators) {
										if ((longestMatchingOp == null || op.name.Length > longestMatchingOp.name.Length) && expression.Substring(charId + 1).StartsWith(op.name)) {
											longestMatchingOp = op;
										}
									}
								}
								bool nextIsSign = canPrecedeNumberInNextGroups.Contains(expression[charId + 1]) || canAlwaysPrecedeNumber.Contains(expression[charId + 1]);
								string stringToTest = (currentGroup.Substring(startsWithSign ? 1 : 0));
								if (nextIsSign || (!Single.TryParse((stringToTest.Substring(0, stringToTest.Length - followingChars) + expression[charId + 1]).Replace(',', '.').Replace(' ', 'X'), NumberStyles.Float, CultureInfo.InvariantCulture, out _) && !canFollowNumber.Contains(expression[charId + 1]))) {
									if (!(currentGroup.Length == 1 && startsWithSign) || nextIsSign || longestMatchingOp != null) {
										if (!DoesNextGroupNeedsCurrentOne(charId, closingGroup)) {
											canCloseGroup = path.Count - 2;
											GetGroupAt(path).linkedGroups.Add(new Group(currentGroup, charId));
										}
									}
								}
							} else {
								GetGroupAt(path).startsWithOp = true;
							}
						}
					}
				}
				if (canCloseGroup != -1) {
					UpdateLastChardIdsOfClosedGroup(path, charId, canCloseGroup);
					path = IncrementOpenLinkedGroup(GetGroupAt(path.GetRange(0, canCloseGroup + 1)), amountToIncrement, path, canCloseGroup, charId);
					
					currentGroup = "";
					madeNewGroup = true;
				}
			}
			
			if (!madeNewGroup && textDepth == 0) {
				// If last characters of current group form an operator, create new groups accordingly
				Operator longestMatchingOp = null; 
				foreach (Operator op in operators) {
					if ((longestMatchingOp == null || op.name.Length > longestMatchingOp.name.Length) && currentGroup.EndsWith(op.name, false, null)) {
						longestMatchingOp = op;
					}
				}
				
				// If there's a longer operator to come, give up
				foreach (Operator op in operators) {
					if (longestMatchingOp == null) break;
					if (op.name.Length <= longestMatchingOp.name.Length) continue;
					
					for (int i = op.name.Length - 2; i >= 0; i--) {
						if (charId - i + op.name.Length > expression.Length) break;
						if (charId - i < 0) continue;
						if (expression.Substring(charId - i, op.name.Length) == op.name) {
							longestMatchingOp = null;
							break;
						}
					}
				}
				
				if (longestMatchingOp != null) {	
					if (longestMatchingOp.name.Length < currentGroup.Length) {
						GetGroupAt(path).linkedGroups.Add(new Group(currentGroup.Substring(0, currentGroup.Length - longestMatchingOp.name.Length), charId - longestMatchingOp.name.Length));
					}
					GetGroupAt(path).linkedGroups.Add(new Group(longestMatchingOp.name));
					path.Add(GetGroupAt(path).linkedGroups.Count - 1);
					GetGroupAt(path).op = longestMatchingOp;
					
					Group newGroup = GetGroupAt(path);
					int openLinkedGroup = 0;
					bool foundOpenLinkedGroup = false;
					foreach (OperatorLinkedGroup linkedGroup in longestMatchingOp.linkedGroups) {
						if (linkedGroup.type == operatorLinkedGroupTypes.explicitString) {
							newGroup.linkedGroups.Add(new Group(linkedGroup.stringVal, charId));
							newGroup.linkedGroups[newGroup.linkedGroups.Count - 1].isExplicitString = (linkedGroup.isImage ? 2 : 1);
							if (!foundOpenLinkedGroup) openLinkedGroup++;
						} else if (linkedGroup.type == operatorLinkedGroupTypes.previousNumber) {
							newGroup.linkedGroups.Add(new Group("\\"));
							Group textGroup = newGroup.linkedGroups[newGroup.linkedGroups.Count - 1];
							int newGroupSiblingId = path[path.Count - 1];
							if (newGroupSiblingId > 0) {
								// Go to previous sibling
								path[path.Count - 1]--;
								Group prevGroup = GetGroupAt(path);
								
								bool foundNumberAsPartOfText = false;
								if (prevGroup.op == null && !(prevGroup.type[0] == '\\' || prevGroup.isExplicitString == 2)) {
									int i = prevGroup.type.Length - 1;
									int followingChars = 0;
									while (i > 0 && ((Single.TryParse(prevGroup.type.Substring(i - 1, prevGroup.type.Length - i + 1 - followingChars).Replace(',', '.').Replace(' ', 'X'), NumberStyles.Float, CultureInfo.InvariantCulture, out _) && !canPrecedeNumberInNextGroups.Contains(prevGroup.type[i - 1])) || (canAlwaysPrecedeNumber.Contains(prevGroup.type[i - 1]) && !canAlwaysPrecedeNumber.Contains(prevGroup.type[i])) || (followingChars == prevGroup.type.Length - i - 1 && canFollowNumber.Contains(prevGroup.type[i])))) {
										if (followingChars == prevGroup.type.Length - i - 1 && canFollowNumber.Contains(prevGroup.type[i])) followingChars++;
										i--;
									}
									if (i != 0) {
										textGroup.linkedGroups.Add(new Group(prevGroup.type.Substring(i), prevGroup.lastCharId));
										prevGroup.lastCharId = prevGroup.lastCharId - textGroup.linkedGroups[textGroup.linkedGroups.Count - 1].type.Length;
										prevGroup.type = prevGroup.type.Substring(0, i);
										// Go back
										path[path.Count - 1]++;
										foundNumberAsPartOfText = true;
									}
								}
								if (!foundNumberAsPartOfText) {
									textGroup.linkedGroups.Add(prevGroup);
									GetGroupAt(path.GetRange(0, path.Count - 1)).linkedGroups.RemoveAt(newGroupSiblingId - 1);
								}
							}
							if (!foundOpenLinkedGroup) openLinkedGroup++;
						} else if (linkedGroup.type == operatorLinkedGroupTypes.previousGroups) {
							newGroup.linkedGroups.Add(new Group("\\"));
							Group textGroup = newGroup.linkedGroups[newGroup.linkedGroups.Count - 1];
							Group parent = GetGroupAt(path.GetRange(0, path.Count - 1));
							while (parent.linkedGroups.Count > 1) {
								textGroup.linkedGroups.Add(parent.linkedGroups[0]);
								parent.linkedGroups.RemoveAt(0);
								path[path.Count - 1]--;
							}
							if (!foundOpenLinkedGroup) openLinkedGroup++;
						} else {
							newGroup.linkedGroups.Add(new Group("\\"));
							foundOpenLinkedGroup = true;
						}
						// Apply absolute size, preset offset and other properties
						Group newLinkedGroup = newGroup.linkedGroups[newGroup.linkedGroups.Count - 1];
						for (int axis = 0; axis < 2; axis++) {
							newLinkedGroup.size[axis] = linkedGroup.sizeAbsolute[axis].ToWU() / 2f;
							newLinkedGroup.size[axis + 2] = linkedGroup.sizeAbsolute[axis].ToWU() / 2f;
							newLinkedGroup.offset[axis] = linkedGroup.offsetAbsolute[axis].ToWU();
						}
					}
					if (openLinkedGroup > GetGroupAt(path).linkedGroups.Count - 1) {
						path = IncrementOpenLinkedGroup(GetGroupAt(path), 1, path, path.Count - 1, charId);
					} else {
						GetGroupAt(path).openLinkedGroup = openLinkedGroup;
						path.Add(openLinkedGroup);
					}	
					currentGroup = "";
					madeNewGroup = true;
				}
			}
			
			// Finally, if on last character of expression, add currentGroup as new group
			if (!madeNewGroup && charId == expression.Length - 1) {
				GetGroupAt(path).linkedGroups.Add(new Group(currentGroup, charId));
				UpdateLastChardIdsOfClosedGroup(path, charId, 0);
			}
		}
		
		// Put back HTML tags
		List<Group> allGroups = GetAllGroups(groups);
		allGroups.Sort((g1, g2) => g1.lastCharId - g2.lastCharId);
		currentTags = new List<string>();
		int groupId = -1;
		int groupCharIdOffset = 0;
		bool italic = false;
		for (int charId = 0; charId < expression.Length || groupId < allGroups.Count - 1; charId++) {
			bool changedGroup = false;
			bool redoingPrevCharId = false;
			if (groupId == -1 || charId > allGroups[groupId].lastCharId) {
				int prevLastCharId = (groupId == -1 ? -1 : allGroups[groupId].lastCharId);
				groupId++;
				while (groupId < allGroups.Count && (allGroups[groupId].lastCharId == -1 || allGroups[groupId].type == "")) groupId++;
				if (groupId == allGroups.Count) break;
				if (allGroups[groupId].lastCharId == prevLastCharId) {
					charId--;
					redoingPrevCharId = true;
				}
				groupCharIdOffset = 0;
				changedGroup = true;
			}
			
			if (!redoingPrevCharId) {
				foreach (string tag in htmlTags[charId].tags ?? Enumerable.Empty<string>()) {
					if (tag[1] == '/' && !(tag == "</i>" && !currentTags.Contains("<i>"))) {
						string tagType = tag.Remove(tag.Length - 1).Substring(2).Split('=')[0];
						currentTags.Reverse();
						int openTagIndex = currentTags.FindIndex((openTag) => openTag.Remove(openTag.Length - 1).Substring(1).Split('=')[0] == tagType);
						if (openTagIndex != -1) currentTags.RemoveAt(openTagIndex);
						currentTags.Reverse();
					} else {
						if (tag == "<i>" && currentTags.Contains("</i>")) currentTags.RemoveAt(currentTags.IndexOf("</i>"));
						else currentTags.Add(tag);
					}
				}
				
				bool italicizeCurrentChar = ((italicize.Contains(expression[charId]) && !currentTags.Contains("<text>") && !currentTags.Contains("</i>")) || currentTags.Contains("<i>"));
				int charIdInGroup = charId - allGroups[groupId].lastCharId + (allGroups[groupId].isExplicitString == 2 ? 1 : allGroups[groupId].type.Length) - 1 - groupCharIdOffset;
				if ((charIdInGroup == 0 || italicizeCurrentChar != italic) && !(allGroups[groupId].isExplicitString != 0 || charIdInGroup < 0)) {
					italic = italicizeCurrentChar;
					if (!(!italic && charIdInGroup == 0)) {
						allGroups[groupId].type = allGroups[groupId].type.Insert(Mathf.Max(charIdInGroup, 0) + groupCharIdOffset, (italic ? "<i>" : "</i>"));
						groupCharIdOffset += (italic ? 3 : 4);
					}
				}
			}
			if (allGroups[groupId].isExplicitString == 2) {
				foreach (string tag in (changedGroup ? currentTags : htmlTags[charId].tags) ?? Enumerable.Empty<string>()) {
					string[] typeValPair = tag.Remove(tag.Length - 1).Substring(1).Split('=');
					if (typeValPair[0] == "color" && typeValPair.Length != 1) {
						Color color;
						if (ColorUtility.TryParseHtmlString(typeValPair[1], out color)) {
							allGroups[groupId].imageColor.Add(color);
						}
					} else if (typeValPair[0] == "/color") {
						if (allGroups[groupId].imageColor.Count != 0) allGroups[groupId].imageColor.RemoveAt(allGroups[groupId].imageColor.Count - 1);
					} else if (typeValPair[0] == "b") {
						allGroups[groupId].imageBold++;
					} else if (typeValPair[0] == "/b") {
						allGroups[groupId].imageBold = Mathf.Max(allGroups[groupId].imageBold - 1, 0);
					} if (typeValPair[0] == "rotate" && typeValPair.Length != 1) {
						float rotation;
						if (Single.TryParse(typeValPair[1].Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out rotation)) {
							allGroups[groupId].imageRotation.Add(rotation);
						}
					} else if (typeValPair[0] == "/rotate") {
						if (allGroups[groupId].imageRotation.Count != 0) allGroups[groupId].imageRotation.RemoveAt(allGroups[groupId].imageRotation.Count - 1);
					} if (typeValPair[0] == "size" && typeValPair.Length != 1) {
						float size;
						int unit = 0;
						if (typeValPair[1].EndsWith("%")) {
							typeValPair[1] = typeValPair[1].Remove(typeValPair[1].Length - 1);
							unit = 1;
						} else if (typeValPair[1].EndsWith("em")) {
							typeValPair[1] = typeValPair[1].Remove(typeValPair[1].Length - 2);
							unit = 2;
						}
						if (Single.TryParse(typeValPair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out size)) {
							allGroups[groupId].imageSize.Add((unit == 1 ? size * 0.01f : (unit == 2 ? size : size * 0.028f)));
						}
					} else if (typeValPair[0] == "/size") {
						if (allGroups[groupId].imageSize.Count != 0) allGroups[groupId].imageSize.RemoveAt(allGroups[groupId].imageSize.Count - 1);
					}
				}
			} else {
				string joinedTags = "";
				foreach (string tag in (changedGroup ? currentTags : htmlTags[charId].tags) ?? Enumerable.Empty<string>()) {
					if (!(tag == "<text>" || tag == "</text>" || (tag == "<i>" && !currentTags.Contains("<i>")))) joinedTags += tag;
				}
				allGroups[groupId].type = allGroups[groupId].type.Insert(Mathf.Max(charId - allGroups[groupId].lastCharId + allGroups[groupId].type.Length - groupCharIdOffset - 1, 0) + groupCharIdOffset, joinedTags);
				groupCharIdOffset += joinedTags.Length;
			}
			
			// Delete non-propagating tags
			for (int i = currentTags.Count - 1; i >= 0; i--) {
				string type = currentTags[i].Remove(currentTags[i].Length - 1).Substring(1).Split('=')[0];
				if (type == "br") currentTags.RemoveAt(i);
			}
		}
		
		// Solve sizes and offsets
		path = new List<int>();
		int sizePasses = 0;
		while (groups.sizeSolved.x != 2 || groups.sizeSolved.y != 2) {
			Group grp = GetGroupAt(path);
			// If group has no linked groups, compute size based on contents (text or image)
			if (grp.linkedGroups.Count == 0) {
				Group parent = GetGroupAt(path.GetRange(0, Mathf.Max(path.Count - 1, 0)));
				if (grp.type != "\\" && grp.op == null) {
					// If group is image, solve size based on texture size
					if (grp.isExplicitString == 2) {
						Sprite sprite = Resources.Load<Sprite>(grp.type);
						if (sprite == null) sprite = missingImage;
						for (int axis = 0; axis < 2; axis++) {
							if (sprite != null) {
								Texture2D texture = sprite.texture;
								float spriteSize = (axis == 0 ? texture.width : texture.height) / groupGameObject.transform.Find("Image").GetComponent<Image>().pixelsPerUnitMultiplier; 
								if (parent.op != null) spriteSize *= Mathf.Abs(parent.op.linkedGroups[path[path.Count - 1]].sizePropToContent[axis]);
								grp.size[axis] += spriteSize / 2f;
								grp.size[axis + 2] += spriteSize / 2f;
							}
						}
						for (int axis = 0; axis < 2; axis++) {
							if (grp.imageBold != 0 && (parent.op == null || parent.op.linkedGroups[path[path.Count - 1]].sizePropToBox[axis] == 0f)) {
								float scaleFactor = (parent.op.linkedGroups[path[path.Count - 1]].sizePropToBox[1 - axis] != 0f || (grp.size.x > grp.size.y) != (axis == 0) ? 1.4f : 1f);
								grp.size[axis] *= scaleFactor;
								grp.size[axis + 2] *= scaleFactor;
							}
						}
						if ((grp.imageSize?.Count ?? 0) != 0) grp.size *= grp.imageSize[grp.imageSize.Count - 1];
					} else {
						groupGameObject.SetActive(true);
						tmp.text = grp.type;
						tmp.ForceMeshUpdate(true);
							
						if (parent.op == null || parent.op.linkedGroups[path[path.Count - 1]].sizePropToContent.x != 0f) {
							Vector2 renderedValues = tmp.GetRenderedValues(false);
							if (parent.op != null) renderedValues *= Mathf.Abs(parent.op.linkedGroups[path[path.Count - 1]].sizePropToContent.x);
							grp.size.x += renderedValues.x / 2f;
							grp.size.z += renderedValues.x / 2f;
						}

						if (parent.op == null || parent.op.linkedGroups[path[path.Count - 1]].sizePropToContent.y != 0f) {
							float maxCharHeight = float.MinValue;
							for (int i = 0; i < tmp.textInfo.characterCount; i++) {
								float charHeight = tmp.textInfo.characterInfo[i].topRight.y;
								if (charHeight > maxCharHeight) maxCharHeight = charHeight;
							}
							grp.size.y += (maxCharHeight - textBaseline) * (parent.op == null ? 1f : Mathf.Abs(parent.op.linkedGroups[path[path.Count - 1]].sizePropToContent.y));
							grp.size.w += textBottomGap * (parent.op == null ? 1f : Mathf.Abs(parent.op.linkedGroups[path[path.Count - 1]].sizePropToContent.y));
						}
						
						groupGameObject.SetActive(false);
					}
				} else {
					grp.size.y -= textBottomGap;
					grp.size.w += textBottomGap;
				}
				for (int axis = 0; axis < 2; axis++) {
					if (parent.op != null) {
						grp.scale[axis] = ((parent.op.linkedGroups[path[path.Count - 1]].sizePropToContent[axis] >= 0f) != (parent.op.linkedGroups[path[path.Count - 1]].sizePropToBox[axis] >= 0f) ? -1f : 1f);
					}
					if (parent.op == null || parent.op.linkedGroups[path[path.Count - 1]].sizePropToBox[axis] == 0f) grp.sizeSolved[axis] = 2;
					else grp.sizeSolved[axis] = 1;
				}
				
				// Go back to parent if any
				if (path.Count > 0) path = path.GetRange(0, path.Count - 1);
			} else {
				// Else, enumerate through all linked groups
				bool areAllLinkedGroupsSolved = true;
				for (int linkedGroupId = 0; linkedGroupId < grp.linkedGroups.Count; linkedGroupId++) {
					if (grp.linkedGroups[linkedGroupId].sizeSolved.x == 0 || grp.linkedGroups[linkedGroupId].sizeSolved.y == 0) {
						// If we haven't tried to solve a linked group size, set path to it
						areAllLinkedGroupsSolved = false;	
						path.Add(linkedGroupId);
						break;
					}
				}
				// If none do, solve size based on them
				if (areAllLinkedGroupsSolved) {
					if (grp.op != null) {
						// Apply last-minute size changes to fully solved groups only
						for (int i = 0; i < grp.linkedGroups.Count; i++) {
							Group linkedGroup = grp.linkedGroups[i];
							if (linkedGroup.sizeSolved.x == 2 && linkedGroup.sizeSolved.y == 2) {
								ApplyMinMaxSizes(linkedGroup, grp.op.linkedGroups[i]);
								ApplyScaleTo(linkedGroup, grp.op.linkedGroups[i], path);
								MultiplyScaleOfAllGroups(linkedGroup, grp.op.linkedGroups[i].scaleMult, false);
							}
							// Also apply text only offset
							if (grp.op != null) {
								Group firstNonParentLinkedGroup = grp.linkedGroups[i];
								while (firstNonParentLinkedGroup.linkedGroups.Count != 0) firstNonParentLinkedGroup = firstNonParentLinkedGroup.linkedGroups[0];
								
								if (firstNonParentLinkedGroup.isExplicitString == 0 && firstNonParentLinkedGroup.type != "\\" && firstNonParentLinkedGroup.type != "") {
									grp.linkedGroups[i].offset.x += grp.op.linkedGroups[i].offsetIfText.x.ToWU();
									grp.linkedGroups[i].offset.y += grp.op.linkedGroups[i].offsetIfText.y.ToWU();
								}
							}
						}
					}
					
					JuxtaposeGroups(grp, true);
					
					// Get absolute size (size of combined juxtaposed-non-proportionally-offset groups)
					Vector2 minCorner = new Vector2(Mathf.Infinity, Mathf.Infinity);
					Vector2 maxCorner = new Vector2(-Mathf.Infinity, -Mathf.Infinity);
					for (int i = 0; i < grp.linkedGroups.Count; i++) {
						for (int axis = 0; axis < 2; axis++) {
							if (grp.linkedGroups[i].sizeSolved[axis] == 2 && (grp.op == null || (grp.op.linkedGroups[i].offsetPropToSize[axis] == 0f && grp.op.linkedGroups[i].offsetPropToBox[axis] == 0f))) {
								minCorner[axis] = Mathf.Min(minCorner[axis], -grp.linkedGroups[i].size[axis + 2] + grp.linkedGroups[i].offset[axis]);
								maxCorner[axis] = Mathf.Max(maxCorner[axis], grp.linkedGroups[i].size[axis] + grp.linkedGroups[i].offset[axis]);
							}
						}
					}
					for (int axis = 0; axis < 2; axis++) {
						if (Single.IsInfinity(minCorner[axis])) minCorner[axis] = 0f;
						if (Single.IsInfinity(maxCorner[axis])) maxCorner[axis] = 0f;
					}
					
					// Solve groups with proportional sizes/offsets
					if (grp.op != null) {
						Vector2 absoluteMinCorner = minCorner;
						Vector2 absoluteMaxCorner = maxCorner;
						Vector2 absoluteSize = absoluteMaxCorner - absoluteMinCorner;
						
						for (int i = 0; i < grp.linkedGroups.Count; i++) {
							Group linkedGroup = grp.linkedGroups[i];
							bool wasntFullySolved = false;
							for (int axis = 0; axis < 2; axis++) {
								if (linkedGroup.sizeSolved[axis] != 2) {
									linkedGroup.size[axis] += absoluteSize[axis] * Mathf.Abs(grp.op.linkedGroups[i].sizePropToBox[axis]) / 2f;
									linkedGroup.size[axis + 2] += absoluteSize[axis] * Mathf.Abs(grp.op.linkedGroups[i].sizePropToBox[axis]) / 2f;
									wasntFullySolved = true;
								}
							}
							if (wasntFullySolved) {
								// Apply last-minute size changes
								ApplyMinMaxSizes(linkedGroup, grp.op.linkedGroups[i]);
								ApplyScaleTo(linkedGroup, grp.op.linkedGroups[i], path);
								MultiplyScaleOfAllGroups(linkedGroup, grp.op.linkedGroups[i].scaleMult, false);
							}
						}
						
						JuxtaposeGroups(grp, false, absoluteMinCorner, absoluteMaxCorner);
						
						for (int i = 0; i < grp.linkedGroups.Count; i++) {
							Group linkedGroup = grp.linkedGroups[i];
							for (int axis = 0; axis < 2; axis++) {
								if ((axis == 0 ? grp.op.linkedGroups[i].originX : grp.op.linkedGroups[i].originY) == originTypes.boxGeometricCenter) linkedGroup.offset[axis] += (absoluteMinCorner[axis] + absoluteMaxCorner[axis]) / 2f;
								linkedGroup.offset[axis] += linkedGroup.size[(grp.op.linkedGroups[i].offsetPropToSize[axis] >= 0f ? axis + 2 : axis)] * grp.op.linkedGroups[i].offsetPropToSize[axis];
								linkedGroup.offset[axis] += (grp.op.linkedGroups[i].offsetPropToBox[axis] >= 0f ? absoluteMaxCorner : absoluteMinCorner)[axis] * Mathf.Abs(grp.op.linkedGroups[i].offsetPropToBox[axis]);
								linkedGroup.sizeSolved[axis] = 2;
								
								minCorner[axis] = Mathf.Min(minCorner[axis], -linkedGroup.size[axis + 2] + linkedGroup.offset[axis]);
								maxCorner[axis] = Mathf.Max(maxCorner[axis], linkedGroup.size[axis] + linkedGroup.offset[axis]);
							}
						}
					}
					grp.size += new Vector4(maxCorner.x, maxCorner.y, -minCorner.x, -minCorner.y);

					// Go back to parent if any, and set sizeSolved
					if (path.Count > 0) {
						int groupLevel = path[path.Count - 1];
						path = path.GetRange(0, path.Count - 1);
						Group parent = GetGroupAt(path);
						grp.sizeSolved = new Vector2Int((parent.op == null || parent.op.linkedGroups[groupLevel].sizePropToBox.x == 0f ? 2 : 1), (parent.op == null || parent.op.linkedGroups[groupLevel].sizePropToBox.y == 0f ? 2 : 1));
					} else {
						grp.sizeSolved = new Vector2Int(2, 2);
					}
				}
			}
			sizePasses++;
			if (sizePasses > 1000) {
				Debug.LogError("Couldn't solve all group sizes !");
				success = false;
				break;
			}
		}

		// Render expression
		for (int i = groupsGameObject.transform.childCount - 1; i >= 0; i--) DestroyImmediate(groupsGameObject.transform.GetChild(i).gameObject);
		renderOffsetSum = Vector2.zero;
		RenderGroup(groups);
		
		duration = Mathf.Floor((Time.realtimeSinceStartup - duration) * 1000f);
		if (success) {
			Debug.Log("Expression successfully parsed in " + duration + "ms.");
		} else {
			Debug.LogError("Couldn't parse expression ! (" + duration + "ms)");
		}
		expression = initialExpression;
	}
	
	public Group GetGroupAt(List<int> path) {
		Group result = groups;
		for (int i = 0; i < path.Count; i++) {
			result = result.linkedGroups[path[i]];
		}
		return result;
	}
	
	public List<Group> GetAllGroups(Group grp) {
		List<Group> allGroups = new List<Group>();
		allGroups.Add(grp);
		foreach (Group linkedGroup in grp.linkedGroups) {
			allGroups.AddRange(GetAllGroups(linkedGroup));
		}
		return allGroups;
	}
	
	public List<int> IncrementOpenLinkedGroup(Group closingGroup, int amountToIncrement, List<int> path, int closingGroupLevel, int charId) {
		bool incremented = false;
		while (!incremented) {
			closingGroup = GetGroupAt(path.GetRange(0, closingGroupLevel + 1));
			bool exceeded = false;
			while (!incremented || closingGroup.op.linkedGroups[closingGroup.openLinkedGroup].type == operatorLinkedGroupTypes.explicitString) {
				closingGroup.openLinkedGroup += amountToIncrement;
				incremented = true;
				if (closingGroup.openLinkedGroup > closingGroup.op.linkedGroups.Length - 1) {
					path = path.GetRange(0, closingGroupLevel);
					exceeded = true;
					if (path.Count != 0) {
						Group parent = GetGroupAt(path.GetRange(0, path.Count - 1));
						if (parent.op.linkedGroups[parent.openLinkedGroup].type == operatorLinkedGroupTypes.nextNumber) {
							if (!DoesNextGroupNeedsCurrentOne(charId, parent)) {
								path = path.GetRange(0, path.Count - 1);
								closingGroupLevel -= 2;
								amountToIncrement = 1;
								incremented = false;
							}
						}
					}
					break;
				}
			}
			if (!exceeded) {
				path = path.GetRange(0, closingGroupLevel + 1);
				path.Add(GetGroupAt(path).openLinkedGroup);
			}
		}
		return path;
	}
	
	public bool DoesNextGroupNeedsCurrentOne(int charId, Group currentGroup) {
		Operator longestMatchingOp = null;
		foreach (Operator op in operators) {
			if ((longestMatchingOp == null || op.name.Length > longestMatchingOp.name.Length) && expression.Substring(charId + 1).StartsWith(op.name)) {
				longestMatchingOp = op;
			}
		}
		if (longestMatchingOp != null && Mathf.Abs(longestMatchingOp.previousGroupTolerance) >= Mathf.Abs(currentGroup.op.previousGroupTolerance) && !(currentGroup.op.previousGroupTolerance < 0 && currentGroup.op == longestMatchingOp)) {
			foreach (OperatorLinkedGroup linkedGroup in longestMatchingOp.linkedGroups) {
				if (linkedGroup.type == operatorLinkedGroupTypes.previousNumber) {
					return true;
				}
			}
		}
		return false;
	}
	
	public void UpdateLastChardIdsOfClosedGroup(List<int> path, int charId, int closedGroupLevel) {
		for (int j = path.Count - 1; j >= closedGroupLevel; j--) {
			Group grp = GetGroupAt(path.GetRange(0, j));
			if (grp.op == null) continue;
			for (int k = grp.openLinkedGroup; k < grp.linkedGroups.Count; k++) {
				if (grp.op.linkedGroups[k].type == operatorLinkedGroupTypes.explicitString) grp.linkedGroups[k].lastCharId = charId;
			}
		}
	}
	
	public void JuxtaposeGroups(Group grp, bool sizeSolved, Vector2? baseGroupMinCorner = null, Vector2? baseGroupMaxCorner = null) {
		for (int axis = 0; axis < 2; axis++) {
			float offset = 0f;
			if (baseGroupMinCorner != null && baseGroupMaxCorner != null) {
				offset += (axis == 0 ? ((Vector2)baseGroupMaxCorner).x : ((Vector2)baseGroupMinCorner).y);
			}
			//Juxtapose (when pass == 0), then center (when pass == 1)
			for (int pass = 0; pass < 2; pass++) {
				for (int i = 0; i < grp.linkedGroups.Count; i++) {
					if ((grp.linkedGroups[i].sizeSolved[axis] == 2) == sizeSolved && ((axis == 0 && grp.op == null) || (grp.op != null && (axis == 0 ? grp.op.linkedGroups[i].originX : grp.op.linkedGroups[i].originY) == originTypes.juxtapose))) {
						if (pass == 0) {
							offset += grp.linkedGroups[i].size[axis + (axis == 0 ? 2 : 0)] * (axis == 0 ? 1f : -1f);
							grp.linkedGroups[i].offset[axis] += offset;
							offset += grp.linkedGroups[i].size[axis + (axis == 0 ? 0 : 2)] * (axis == 0 ? 1f : -1f);
						} else if (baseGroupMinCorner == null || baseGroupMaxCorner == null) {
							grp.linkedGroups[i].offset[axis] -= offset / 2f;
						}
					}
				}
			}
		}
	}
	
	public void ApplyMinMaxSizes(Group grp, OperatorLinkedGroup opLinkedGroup) {
		for (int axis = 0; axis < 2; axis++) {
			for (int minMax = 0; minMax < 2; minMax++) {
				float grpSize = grp.size[axis] + grp.size[axis + 2];
				float targetSize = (minMax == 0 ? opLinkedGroup.sizeMin : opLinkedGroup.sizeMax)[axis].ToWU();
				if (targetSize != 0f && (minMax == 0 ? grpSize < targetSize : grpSize > targetSize)) {
					grp.size[axis] += targetSize - grpSize;
				}
			}
		}
	}
	
	public void ApplyScaleTo(Group grp, OperatorLinkedGroup opLinkedGroup, List<int> path) {
		if (grp.gotScaledTo) return;
		float smallestRatio = Mathf.Infinity;
		int smallestRatioAxis = -1;
		int fitAxes = 0;
		for (int axis = 0; axis < 2; axis++) {
			if (opLinkedGroup.scaleTo[axis].val != 0f) {
				fitAxes++;
				float ratio = opLinkedGroup.scaleTo[axis].ToWU() / Mathf.Max((grp.size[axis] + grp.size[axis + 2]), 0.0001f);
				if (ratio < smallestRatio) {
					smallestRatio = ratio;
					smallestRatioAxis = axis;
				}
			}
		}	
		if (fitAxes != 0) {
			MultiplyScaleOfAllGroups(grp, smallestRatio, false);
			GetGroupAt(path.GetRange(0, path.Count - 1)).gotScaledTo = true;
			if (fitAxes == 2) {
				float deltaSize = opLinkedGroup.scaleTo[1 - smallestRatioAxis].ToWU() - (grp.size[1 - smallestRatioAxis] + grp.size[1 - smallestRatioAxis + 2]);
				grp.size[1 - smallestRatioAxis] += deltaSize / 2f;
				grp.size[1 - smallestRatioAxis + 2] += deltaSize / 2f;
			}
		}
	}
	
	public void MultiplyScaleOfAllGroups(Group grp, float scaleMult, bool scaleOffset) {
		grp.size *= scaleMult;
		grp.scale *= scaleMult;
		if (scaleOffset) grp.offset *= scaleMult;
		
		foreach (Group linkedGroup in grp.linkedGroups) {
			MultiplyScaleOfAllGroups(linkedGroup, scaleMult, true);
		}
	}
	
	public void RenderGroup(Group grp) {
		renderOffsetSum += grp.offset;
		// Render only text groups (except if debug mode)
		if (!(grp.type == "\\" || grp.op != null) || debug) {
			GameObject instance = Instantiate(groupGameObject, groupsGameObject.transform);
			
			instance.GetComponent<RectTransform>().localPosition = renderOffsetSum + new Vector2((grp.size.x - grp.size.z) / 2f, (grp.size.y - grp.size.w) / 2f);
			instance.GetComponent<TMPro.TextMeshProUGUI>().fontSize *= Mathf.Abs(grp.scale.y);
			instance.transform.Find("Image").GetComponent<Image>().pixelsPerUnitMultiplier /= Mathf.Abs(grp.scale.y);
			instance.GetComponent<RectTransform>().sizeDelta = new Vector2(grp.size.x + grp.size.z, grp.size.y + grp.size.w);
			instance.GetComponent<RectTransform>().localScale = new Vector2(Mathf.Sign(grp.scale.x), Mathf.Sign(grp.scale.y));
			
			if (grp.type == "\\" || grp.op != null) {
				instance.GetComponent<TMPro.TextMeshProUGUI>().text = "";
			} else if (grp.isExplicitString == 2) {
				instance.GetComponent<TMPro.TextMeshProUGUI>().text = "";
				instance.transform.Find("Image").GetComponent<Image>().sprite = Resources.Load<Sprite>(grp.type);
				if (instance.transform.Find("Image").GetComponent<Image>().sprite == null) instance.transform.Find("Image").GetComponent<Image>().sprite = missingImage;
				instance.transform.Find("Image").GetComponent<Image>().color = ((grp.imageColor?.Count ?? 0) == 0 ? Color.white : grp.imageColor[grp.imageColor.Count - 1]);
				instance.transform.Find("Image").eulerAngles = new Vector3(0f, 0f, ((grp.imageRotation?.Count ?? 0) == 0 ? 0f : grp.imageRotation[grp.imageRotation.Count - 1]));
				instance.transform.Find("Image").gameObject.SetActive(true);
			} else {
				instance.GetComponent<TMPro.TextMeshProUGUI>().text = grp.type;
			}
			
			if (debug) {
				instance.transform.Find("Border").GetComponent<Image>().color = Color.HSVToRGB(UnityEngine.Random.value, 1f, 1f);
			}
			instance.transform.Find("Border").gameObject.SetActive(debug);
			
			instance.SetActive(true);
		}
		
		foreach (Group linkedGroup in grp.linkedGroups) {
			RenderGroup(linkedGroup);
		}
		renderOffsetSum -= grp.offset;
	}
}

public enum operatorLinkedGroupTypes {closingString, previousNumber, nextNumber, explicitString, previousGroups, nextGroups};
public enum unitTypes {worldUnits, lineHeights, characterWidths};
public enum originTypes {juxtapose, boxSizeCenter, boxGeometricCenter};

[System.Serializable] public class TagList {
	#nullable enable
	[HideInInspector] public List<string>? tagsLazy;
	[HideInInspector] public List<string> tags => tagsLazy ??= new List<string>();
	#nullable disable
}

[System.Serializable] public class Group {
	public string type;
	[HideInInspector] public Operator op;
	public List<Group> linkedGroups;
	[HideInInspector] public int openLinkedGroup;
	
	public Vector4 size;
	[HideInInspector] public Vector2 scale;
	public Vector2 offset;
	
	[HideInInspector] public int isExplicitString;
	[HideInInspector] public bool startsWithOp;
	[HideInInspector] public Vector2Int sizeSolved;
	[HideInInspector] public bool gotScaledTo;
	[HideInInspector] public int lastCharId;
	
	[HideInInspector] public int imageBold;
	#nullable enable
	[HideInInspector] public List<Color32>? imageColorLazy;
	[HideInInspector] public List<Color32> imageColor => imageColorLazy ??= new List<Color32>();
	[HideInInspector] public List<float>? imageRotationLazy;
	[HideInInspector] public List<float> imageRotation => imageRotationLazy ??= new List<float>();
	[HideInInspector] public List<float>? imageSizeLazy;
	[HideInInspector] public List<float> imageSize => imageSizeLazy ??= new List<float>();
	#nullable disable
	
	public Group(string cType, int cLastCharId = -1) {
		this.type = cType;
		this.op = null;
		this.linkedGroups = new List<Group>();
		this.openLinkedGroup = 0;
		
		this.size = Vector4.zero;
		this.scale = Vector2.one;
		this.offset = Vector2.zero;
		
		this.isExplicitString = 0;
		this.startsWithOp = false;
		this.sizeSolved = Vector2Int.zero;
		this.gotScaledTo = false;
		this.lastCharId = cLastCharId;
		
		this.imageBold = 0;
	}
}

[System.Serializable] public class Operator {
	public string name;
	public OperatorLinkedGroup[] linkedGroups;
	public int previousGroupTolerance;
}

[System.Serializable] public class FloatUnitPair {
	public float val;
	public unitTypes unit;
	
	public float ToWU() {
		switch (this.unit) {
			case unitTypes.worldUnits : return this.val;
			case unitTypes.lineHeights : return this.val * MathExpressionRenderer.textLineHeight;
			case unitTypes.characterWidths : return this.val * MathExpressionRenderer.textCharWidth;
		}
		return 0f;
	}
}

[System.Serializable] public class Vector2FloatUnitPair {
	public FloatUnitPair x;
	public FloatUnitPair y;
	
    public FloatUnitPair this[int index] {
        get {
            return index switch {
                0 => x,
                1 => y,
                _ => throw new IndexOutOfRangeException("Index must be 0 or 1.")
            };
        }
        set {
            switch (index) {
                case 0: x = value; break;
                case 1: y = value; break;
                default: throw new IndexOutOfRangeException("Index must be 0 or 1.");
            }
        }
    }
}
	
[System.Serializable] public class OperatorLinkedGroup {
	public operatorLinkedGroupTypes type;
	public bool isImage;
	public string stringVal;
	
	public bool sizeFoldout;
	public Vector2 sizePropToContent;
	public Vector2FloatUnitPair sizeAbsolute;
	public Vector2 sizePropToBox;
	public Vector2FloatUnitPair sizeMin;
	public Vector2FloatUnitPair sizeMax;
	public Vector2FloatUnitPair scaleTo;
	public float scaleMult;
	
	public bool offsetFoldout;
	public originTypes originX;
	public originTypes originY;
	public Vector2FloatUnitPair offsetAbsolute;
	public Vector2FloatUnitPair offsetIfText;
	public Vector2 offsetPropToSize;
	public Vector2 offsetPropToBox;
}

[System.Serializable] public class CharacterShorthand {
	public string key;
	public string val;
}
