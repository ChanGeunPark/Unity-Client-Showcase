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


---

## 2. 폴더 구조

핵심 코드는 `**Assets/@Assets*`\* 아래에 있습니다.

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

1. `**GameDataEventBus`\*\* (`Scripts/Backend/GameData/`)

- 데이터 변경 이벤트 발행. Batch(BeginBatch~EndBatch)로 한 번에 알림.

2. `**GameDataStore**` (`Scripts/Backend/GameData/`)

- 인벤토리/통화 등 테이블 보관. 로직 없이 보관만.

3. `**GameDataManager**` (`Scripts/Managers/`)

- Store + EventBus 진입점. Singleton, 이벤트 포워딩.

4. `**BackendManager**` (`Scripts/Managers/BackendManager/`)

- 백엔드 연동·테이블 제공. 부분 클래스로 기능 분리.

5. `**UIManager**` (`Scripts/Managers/`)

- 팝업/UI 생명주기, EventBus 구독 연동.

6. `**BackendChart**` (`Scripts/Backend/Services/`)

- CSV 차트 로드. `GetChartFromLocal(chartName)` → Resources/Charts/{chartName}.csv 로드 후 JsonData 변환.

7. `**BackendChartHelper**` (`Scripts/Backend/Services/Helper/`)

- 차트 파싱: `LoadCharacterChart`(Name, Grade), `LoadCharacterGachaProbability`(CharacterId, Probability → Dictionary<string, float>).

8. `**GachaCtrl**` (`Scripts/Backend/Controller/`)

- `DoCharacterGacha(gachaCount)`: Store의 GachaProbabilityData 가중치로 랜덤 뽑기, CharacterChart와 매칭해 리스트 반환.

9. `**ResourceManager**` 스프라이트: `LoadSpriteByKeyAsync(key)`로 캐시 로드, `ReleaseSpriteHandlesByKey()`로 키 기반 스프라이트만 해제. 씬 전환 시 해제 호출 권장.

---

## 4. 차트·가챠

- **차트 로드**: `BackendManager.GetChartContents(chartName)` → BackendChart가 CSV 로드 후 Helper에서 타입별 파싱.
  - `CharacterChart`: 캐릭터 목록 (Name, Grade).
  - `CharacterGachaProbability`: 캐릭터별 가챠 확률 (CharacterId, Probability).
- **초기화**: `InitializeGameData()`에서 위 두 차트를 한 번 로드해 Store에 넣음. Reset 시 null로 비운 뒤 재로드.
- **가챠**: `GachaCtrl.DoCharacterGacha(gachaCount)` — 확률 비율에 맞게 가중치 랜덤으로 `gachaCount`번 뽑아 `List<CharacterChart>` 반환.

---

## 5. 연출 / 데모

- **가챠 인트로 연출**: `GachaPopupUI` + `GachaIntroHolder`
  - `GachaPopupUI`: 가챠 시작 → 카드 선택 → 별똥별 → 포탈까지 전체 플로우를 관리합니다.
  - `GachaIntroHolder`: 카드 한 장의 연출(연기, 카드 플립, 레인보우 효과, 스프레드, 티어별 파티클)을 담당합니다.
  - 두 컴포넌트 모두 `GachaPopupConfig`, `GachaCardRevealConfig` ScriptableObject를 통해 타이밍·이펙트 강도·티어별 설정을 주입받도록 설계했습니다.
  - 기획이 요청하는 “강한/약한 연출”, “레전드만 특별하게” 같은 요구사항을 코드 수정 없이 Config 에셋 값만 조정해서 반영할 수 있습니다.
- **인벤토리 / 기타 UI**: `MainUI`, `InventoryPopupUI`, `CharacterHolder` 등은 공통 `BaseUI`, `BasePopupUI`를 상속받아, 등장/퇴장 애니메이션과 EventBus 기반 데이터 반영 패턴을 공유합니다.

TODO: GIF추가.

## 사용 스택

- Unity
- UniTask (비동기·연출)
- DOTween (연출)
- 로컬 저장: PlayerPrefs / JSON (서버 연동은 교체 가능 구조)

### 프로젝트 실행을 위한 외부 에셋

이 리포지토리는 **코드와 구조**를 보여주기 위한 예시이며, 실제 연출/머티리얼/이펙트는 아래 Unity Asset Store 패키지에 의존합니다.  
프로젝트를 그대로 실행해 보고 싶다면 다음 에셋들을 먼저 임포트해야 합니다.

- **AllIn1SpriteShader**
- **Cartoon VFX Deluxe**
- **DOTween Pro**
- **UI ParticleImage**
- **UIFX Bundle**

위 에셋들이 없는 상태에서도 코드를 읽고 구조를 이해하는 데에는 문제가 없지만,  
실행 시에는 일부 머티리얼·셰이더·파티클 프리팹이 누락되어 연출이 깨지거나 에러가 발생할 수 있습니다.
