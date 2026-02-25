using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "CharacterDataSO", menuName = "Scriptable Objects/CharacterDataSO")]
public class CharacterDataSO : ScriptableObject
{
  public string CharacterId;
  public string CharacterName;
  public AssetReferenceSprite ProfileImage;
  public AssetReferenceSprite FaceImage;

  [TextArea(3, 10)]
  public string Description;
}
