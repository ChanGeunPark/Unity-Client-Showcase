# Unity Client Showcase

코드 설계와 가독성을 보여주기 위한 예시 프로젝트입니다.  
(실제 출시 경험은 포트폴리오의 **두근두근 냥빵**에서 확인하실 수 있습니다.)

---

## 1. 아키텍처

### 설계 방향

- **데이터 계층**: Store(보관) + EventBus(변경 알림)로 분리해, UI/가챠 등은 “데이터 직접 접근”이 아니라 “이벤트 구독”으로 반응합니다.
- **의존성**: 매니저/컨트롤러는 Singleton 또는 ServiceLocator로 진입점을 두고, 실제 데이터/로직은 Backend·Data 폴더에 모아 확장과 테스트를 쉽게 합니다.

> 실제 서비스 환경에서는 Firebase 또는 서버 API로 교체 가능한 구조로 설계했습니다.  
> 현재는 로컬(PlayerPrefs/JSON) 저장으로 동작합니다.

### 다이어그램 (예시)

```
[UI Layer]  MainUI, Popup, Holder
      │         │ 구독
      ▼         ▼
[Managers]  UIManager, GameDataManager
      │         │ Store + EventBus
      ▼         ▼
[Backend]   Controller → Services / GameData(Store, EventBus)
      │
      ▼
[Data]      Model, Handler, SO (ScriptableObject)
```

_(프로젝트에 맞게 Mermaid 등으로 그려 두시면 면접 시 설명하기 좋습니다.)_

---

## 2. 폴더 구조

핵심 코드는 **`Assets/@Assets`** 아래에 있습니다.

| 경로                               | 역할                                                                                                                          |
| ---------------------------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| **Scripts/Backend**                | 서버/데이터 연동 역할. `Controller`(API 호출·데이터 갱신), `GameData`(Store·EventBus·비교), `Services`(테이블/비즈니스 로직). |
| **Scripts/Controller**             | 씬·오브젝트 제어용 (이미지, 유틸 등). Backend와 구분해 “화면/입력” 쪽 로직.                                                   |
| **Scripts/Data**                   | 데이터 정의·처리. `Model`(테이블 구조), `Handler`(저장/로드), `Items`(에셋), `SO`(ScriptableObject 스크립트).                 |
| **Scripts/Managers**               | 전역 진입점. GameDataManager(Store+EventBus), UIManager, BackendManager(폴더로 분리), ResourceManager, LocalDataManager.      |
| **Scripts/UI**                     | UI 전용. `Core`(BaseUI, BasePopupUI), `Holder`, `Main`, `Popup` — Prefabs/UI와 이름을 맞춰 두었습니다.                        |
| **Scripts/Editor**                 | Unity 에디터 확장.                                                                                                            |
| **Prefabs/UI**                     | Holder, Popup 등 UI 프리팹. Scripts/UI와 1:1 대응.                                                                            |
| **Sprites/Sprite_UI/HUD**          | HUD용 스프라이트.                                                                                                             |
| **Scenes, Resources, Audio, Font** | 씬·리소스·사운드·폰트.                                                                                                        |

---

## 3. 핵심 클래스 (읽어보면 좋은 순서)

1. **`GameDataEventBus`** (`Scripts/Backend/GameData/`)
   - 데이터 변경 이벤트 발행. Batch(BeginBatch~EndBatch)로 한 번에 알림.
2. **`GameDataStore`** (`Scripts/Backend/GameData/`)
   - 인벤토리/통화 등 테이블 보관. 로직 없이 보관만.
3. **`GameDataManager`** (`Scripts/Managers/`)
   - Store + EventBus 진입점. Singleton, 이벤트 포워딩.
4. **`BackendManager`** (`Scripts/Managers/BackendManager/`)
   - 백엔드 연동·테이블 제공. 부분 클래스로 기능 분리.
5. **`UIManager`** (`Scripts/Managers/`)
   - 팝업/UI 생명주기, EventBus 구독 연동.
6. **`BackendChart`** (`Scripts/Backend/Services/`)
   - CSV 차트 로드. `GetChartFromLocal(chartName)` → Resources/Charts/{chartName}.csv 로드 후 JsonData 변환.
7. **`BackendChartHelper`** (`Scripts/Backend/Services/Helper/`)
   - 차트 파싱: `LoadCharacterChart`(Name, Grade), `LoadCharacterGachaProbability`(CharacterId, Probability → Dictionary<string, float>).
8. **`GachaCtrl`** (`Scripts/Backend/Controller/`)
   - `DoCharacterGacha(gachaCount)`: Store의 GachaProbabilityData 가중치로 랜덤 뽑기, CharacterChart와 매칭해 리스트 반환.
9. **`ResourceManager`** 스프라이트: `LoadSpriteByKeyAsync(key)`로 캐시 로드, `ReleaseSpriteHandlesByKey()`로 키 기반 스프라이트만 해제. 씬 전환 시 해제 호출 권장.

---

## 4. 차트·가챠

- **차트 로드**: `BackendManager.GetChartContents(chartName)` → BackendChart가 CSV 로드 후 Helper에서 타입별 파싱.
  - `CharacterChart`: 캐릭터 목록 (Name, Grade).
  - `CharacterGachaProbability`: 캐릭터별 가챠 확률 (CharacterId, Probability).
- **초기화**: `InitializeGameData()`에서 위 두 차트를 한 번 로드해 Store에 넣음. Reset 시 null로 비운 뒤 재로드.
- **가챠**: `GachaCtrl.DoCharacterGacha(gachaCount)` — 확률 비율에 맞게 가중치 랜덤으로 `gachaCount`번 뽑아 `List<CharacterChart>` 반환.

---

## 5. 연출 / 데모

**TODO:: 가챠·인벤토리 연출 추가.**

## 사용 스택

- Unity
- UniTask (비동기·연출)
- DOTween (연출)
- 로컬 저장: PlayerPrefs / JSON (서버 연동은 교체 가능 구조)
