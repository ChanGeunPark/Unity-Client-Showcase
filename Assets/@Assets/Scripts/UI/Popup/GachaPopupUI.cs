using System.Collections;
using System.Collections.Generic;
using System.Linq;

using AirFishLab.ScrollingList;

using AssetKits.ParticleImage;

using DG.Tweening;


using UnityEngine;
using UnityEngine.UI;

public class GachaPopupUI : BasePopupUI
{
    enum Objects
    {

        CircularWithAligning,
        ListBank,
        SmokeAni,
        FrontStonCats,
        // 배경.
        BackgroundGroup,
        Gress,
        BackGroup,
        MainGroup,
        LeftLeep,
        RightLeep,
        Potal,
        ShootingStar, // 별똥별
        Bell,
        ShootingStarEffect, // 별똥별 부딛힐때
        Sunlight, // 태양빛
        Step1,

        SeowonPopup,
        ShootingStarTail,
        ShootingStarIcon,
        CatFace
    }

    enum Images
    {

        WhiteOverlay,
        Step1Background,
        GachaStoryImage,

    }

    enum Texts
    {
        GachaSpeachText,
    }

    enum Buttons
    {
        SkipButton,
    }




    [SerializeField] private GameObject _smokeAnimation;
    [SerializeField] private GameObject _afterPortalGroup;
    [SerializeField] private GameObject _step1;
    // [SerializeField] private GameObject _step2;
    // [SerializeField] private GameObject _step3;

    private Coroutine _catFaceBlinkRoutine;


    private CircularScrollingList circularScrollingList;
    private ListBank listBank;

    private int _tier = 0;

    private int currentItemCount;
    private CharacterChart _mainGachaCharacterChart;
    private List<CharacterChart> _gachaCharacterChartList;

    private int _currentStep = 1;
    private CharacterChart _currentGachaCharacterChart;

    private bool _isProcessingClick = false;
    // public List<GachaStep2> _gachaStep2List;
    // public List<GachaStep3> _gachaStep3List;


    protected override void OnDestroy()
    {
        if (circularScrollingList != null && circularScrollingList.ListSetting != null)
            circularScrollingList.ListSetting.OnMovementEnd.RemoveListener(OnScrollEnded);
        if (circularScrollingList != null)
        {
            foreach (var item in circularScrollingList.ListBoxes)
            {
                if (item != null)
                {
                    var holder = item.GetComponent<GachaIntroHolder>();
                    if (holder != null)
                        holder.OnClick -= OnItemClicked;
                }
            }
        }
        base.OnDestroy();
    }

    private void Awake()
    {
        BindObject(typeof(Objects));
        BindImage(typeof(Images));
        BindText(typeof(Texts));
        BindButton(typeof(Buttons));
        GetObject(Objects.CircularWithAligning).gameObject.SetActive(false);

        _afterPortalGroup.gameObject.SetActive(false);
        _step1.gameObject.SetActive(false);
        // _step2.gameObject.SetActive(false);
        // _step3.gameObject.SetActive(false);
        GetImage(Images.WhiteOverlay).gameObject.SetActive(false);
        GetObject(Objects.Potal).gameObject.SetActive(false);
        GetObject(Objects.ShootingStarEffect).gameObject.SetActive(false);
        GetObject(Objects.ShootingStar).gameObject.SetActive(false);
        GetObject(Objects.Sunlight).gameObject.SetActive(false);
        HideButton(Buttons.SkipButton);

        GetButton(Buttons.SkipButton).onClick.AddListener(OnSkipButtonClicked);
        // GetObject(Objects.Step3FadeIn).gameObject.SetActive(false);

    }


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            Close();
        }
    }

    private void Start()
    {
        List<CharacterChart> gachaObjectList = new List<CharacterChart>()
        {
            new CharacterChart()
            {
                Grade = CharacterGrade.Rare,
                CharacterId = "lucius",
            },
            new CharacterChart()
            {
                Grade = CharacterGrade.Epic,
                CharacterId = "lime",
            },
            new CharacterChart()
            {
                Grade = CharacterGrade.Legendary,
                CharacterId = "hermiyu",
            },
        };

        Initialize(gachaObjectList);

    }


    public void Initialize(List<CharacterChart> gachaCharacterChartList)
    {



        // _gachaObjectList = gachaObjectList
        //     .OrderByDescending(g => g.itemType == GachaObjectType.Character)
        //     .ThenByDescending(g => g.tier)
        //     .ToList();
        _gachaCharacterChartList = gachaCharacterChartList;

        _currentGachaCharacterChart = _gachaCharacterChartList.FirstOrDefault();
        if (_gachaCharacterChartList != null && _gachaCharacterChartList.Count > 0)
        {
            _mainGachaCharacterChart = _gachaCharacterChartList.OrderBy(g => g.Grade).FirstOrDefault();

            Debug.Log($"Main Gacha Object: {_mainGachaCharacterChart.CharacterId}");
        }


        foreach (var item in _gachaCharacterChartList)
        {
            Debug.Log($"Gacha Object: {item.CharacterId}, {item.Grade}");
        }

        _tier = _mainGachaCharacterChart.Grade == CharacterGrade.Legendary ? 1 : _mainGachaCharacterChart.Grade == CharacterGrade.Epic ? 2 : 3;

        InitAnimation();

        DOTween.Sequence().AppendInterval(1f).AppendCallback(() =>
        {
            _step1.gameObject.SetActive(true);
            GetImage(Images.Step1Background).DOFade(0.95f, 0.5f).From(0).SetEase(Ease.Linear).SetAutoKill(true);
            GetObject(Objects.CircularWithAligning).gameObject.SetActive(true);

            // 카드 리스트 (CircularScrollingList는 활성화된 다음 프레임에 Start()에서 초기화되므로, 리스트 박스 구독은 1프레임 뒤에 수행)
            circularScrollingList = GetObject(Objects.CircularWithAligning).GetComponent<CircularScrollingList>();
            listBank = GetObject(Objects.ListBank).GetComponent<ListBank>();
            _smokeAnimation.gameObject.SetActive(false);

            // 재진입 시 중복 리스너 방지: 기존 구독 제거 후 추가
            circularScrollingList.ListSetting.OnMovementEnd.RemoveListener(OnScrollEnded);
            circularScrollingList.ListSetting.OnMovementEnd.AddListener(OnScrollEnded);
            currentItemCount = circularScrollingList.ListBank.GetContentCount();

            StartCoroutine(SubscribeToHoldersNextFrame());
        });
    }

    private IEnumerator SubscribeToHoldersNextFrame()
    {
        yield return null; // CircularScrollingList.Start() → Initialize() → SetListBoxes() 실행 후 ListBoxes가 채워지도록 1프레임 대기
        if (circularScrollingList == null) yield break;
        foreach (var item in circularScrollingList.ListBoxes)
        {
            if (item == null) continue;
            var holder = item.GetComponent<GachaIntroHolder>();
            if (holder != null)
            {
                holder.OnClick -= OnItemClicked;
                holder.OnClick += OnItemClicked;
                holder.Tier = _tier;
            }
        }
    }

    private void InitAnimation()
    {
        GetObject(Objects.MainGroup).transform.DOScale(1f, 1f).From(1.2f).SetEase(Ease.OutQuart).SetAutoKill(true);
        GetObject(Objects.Gress).transform.DOScale(1f, 1f).From(0.8f).SetEase(Ease.OutQuart).SetAutoKill(true);
        // GetObject(Objects.Gress).transform.DOMoveY(0, 1f).From(-50f).SetRelative(true).SetEase(Ease.Linear).SetAutoKill(true);

        GetObject(Objects.FrontStonCats).transform.DOScale(1.1f, 1f).From(0.8f).SetRelative(true).SetEase(Ease.OutQuart).SetAutoKill(true);


        GetObject(Objects.LeftLeep).transform.DOScale(1, 1f).From(1.2f).SetRelative(true).SetEase(Ease.OutQuart).SetAutoKill(true);
        GetObject(Objects.RightLeep).transform.DOScale(1, 1f).From(1.2f).SetRelative(true).SetEase(Ease.OutQuart).SetAutoKill(true);

        if (_catFaceBlinkRoutine == null)
        {
            _catFaceBlinkRoutine = StartCoroutine(CatFaceBlinkRoutine());
        }
    }

    private IEnumerator CatFaceBlinkRoutine()
    {
        while (true)
        {
            GetObject(Objects.CatFace).SetActive(true);
            yield return new WaitForSeconds(4f);
            GetObject(Objects.CatFace).SetActive(false);
            yield return new WaitForSeconds(1f);
        }
    }

    private void OnScrollEnded()
    {
        UpdateUIAfterScroll();
    }

    private void UpdateUIAfterScroll()
    {
        int focusingContentID = circularScrollingList.GetFocusingContentID();
    }


    private void OnItemClicked(GachaIntroHolder gachaIntroHolder)
    {
        if (_isProcessingClick) return;
        _isProcessingClick = true;

        DelayAction(() =>
        {
            ShowButton(Buttons.SkipButton);
        }, 1.5f);

        // Disable scrolling for the circular scrolling list
        circularScrollingList.enabled = false;

        // Optionally, you can also disable the drag functionality if it's separate
        if (circularScrollingList.GetComponent<ScrollRect>() != null)
        {
            circularScrollingList.GetComponent<ScrollRect>().enabled = false;
        }

        List<float> randomDelay = new List<float> { 0, 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f };
        foreach (var item in circularScrollingList.ListBoxes)
        {

            if (item == gachaIntroHolder)
            {
                gachaIntroHolder.transform.SetAsLastSibling();
                gachaIntroHolder.GetComponent<GachaIntroHolder>().HideCardName();
                // seowonHolder.GetComponent<SeowonHolder>().Initialize(_tier);
                // seowonHolder.transform.DOLocalMoveY(1000, 0.5f).SetEase(Ease.InOutQuad).SetAutoKill(true).SetDelay(3f);
                // GetObject(Objects.EnergySpread).transform.DOLocalMoveY(1000, 0.5f).SetEase(Ease.InOutQuad).SetAutoKill(true).SetDelay(3f);
            }
            else
            {

                item.GetComponent<GachaIntroHolder>().FadeOutImage();
                item.GetComponent<GachaIntroHolder>().HideCardName();
                float selectedDelay = randomDelay[Random.Range(0, randomDelay.Count)];
                randomDelay.Remove(selectedDelay);
                item.transform.DOLocalMoveY(1000, 0.4f).SetEase(Ease.InOutQuad).SetAutoKill(true).SetDelay(selectedDelay).OnComplete(() =>
                {
                    item.gameObject.SetActive(false);
                });
            }
        }

        DelayAction(() =>
        {
            _smokeAnimation.gameObject.SetActive(true);
        }, 3.4f);


        DelayAction(() =>
        {
            GetImage(Images.Step1Background).DOFade(0, 1f).SetAutoKill(true);
            GachaAnimation();
        }, 3.5f);



    }


    public void GachaAnimation()
    {
        DelayAction(() =>
        {
            GetObject(Objects.Sunlight).gameObject.SetActive(true);
            GetObject(Objects.ShootingStar).gameObject.SetActive(true);
            ParticleImage starsParticle = GetObject(Objects.ShootingStar).GetComponent<ParticleImage>();

            if (_tier == 1)
            {
                starsParticle.textureSheetFrameSpeedRange = new SpeedRange(0, 5);
            }
            else if (_tier == 2 || _tier == 3)
            {
                starsParticle.textureSheetFrameSpeedRange = new SpeedRange(4, 4);
            }
            else
            {
                starsParticle.textureSheetFrameSpeedRange = new SpeedRange(2, 2);
            }


            GetObject(Objects.ShootingStar).transform.Find("ShootingStarImage").GetComponent<DOTweenAnimation>().DOPlayForward();

            GetObject(Objects.ShootingStarTail).transform.DOScaleY(0, 2f).SetDelay(0.5f).SetEase(Ease.OutQuart).SetAutoKill(true);


            var starPosition = GetObject(Objects.Bell).transform.position;
            starPosition.y += -100;


            Debug.Log("Shooting Star Move Start");
            // 별똥별 떨어지는 애니메이션
            GetObject(Objects.ShootingStar).transform.DOMove(starPosition, 1f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                Debug.Log("Bell Animation Start");
                GetObject(Objects.ShootingStar).GetComponent<ParticleImage>().lifetime = 0;
                GetObject(Objects.ShootingStarEffect).gameObject.SetActive(true);
                GetObject(Objects.ShootingStar).transform.Find("ShootingStarImage").gameObject.SetActive(false);

                Debug.Log("Bell Animation Start2");
                // 별똥별 파티클 끄기
                DelayAction(() =>
                {
                    GetObject(Objects.ShootingStar).SetActive(false);
                    GetObject(Objects.ShootingStarEffect).SetActive(false);
                }, 2f);


                Debug.Log("Bell Animation Start3");
                // 종 애니메이션 재생
                var bellAnimationSequence = DOTween.Sequence().SetTarget(GetObject(Objects.Bell));
                bellAnimationSequence.Append(GetObject(Objects.Bell).transform.DOLocalRotate(new Vector3(0, 0, -25), 1f).SetEase(Ease.InOutQuad));
                bellAnimationSequence.Append(GetObject(Objects.Bell).transform.DOLocalRotate(new Vector3(0, 0, 25), 1f).SetEase(Ease.InOutQuad));
                bellAnimationSequence.SetLoops(-1, LoopType.Yoyo).SetAutoKill(true);

                // MainGroup 흔들기 애니메이션
                GetObject(Objects.MainGroup).transform.DOShakePosition(0.5f, 10, 23, 90, true, true).SetEase(Ease.InQuad).SetAutoKill(true);
            });

        }, 1f);

        DelayAction(() =>
        {
            // 포탈 애니메이션 시작
            GetObject(Objects.Step1).gameObject.SetActive(false);
            GoPortal();
        }, 2.5f);
    }

    public void GoPortal()
    {
        GetObject(Objects.Potal).gameObject.SetActive(true);
        GetObject(Objects.Potal).transform.DOScale(1, 0.3f).SetEase(Ease.OutBack).From(0).SetAutoKill(true);

        DelayAction(() =>
        {
            // 포탈 열린후 애니메이션
            _afterPortalGroup.gameObject.SetActive(true);
            _afterPortalGroup.transform.DOScale(1, 1f).SetEase(Ease.InOutQuart).From(0).SetAutoKill(true);


            ParticleImage starsParticle = _afterPortalGroup.transform.Find("EnergySpread").Find("Stars").GetComponent<ParticleImage>();
            if (_tier == 1)
            {
                starsParticle.textureSheetFPS = 3;
            }
            else if (_tier == 2 || _tier == 3)
            {
                starsParticle.textureSheetStartFrame = 4;
            }
            else
            {
                starsParticle.textureSheetStartFrame = 2;
            }





            GetObject(Objects.MainGroup).transform.DOScale(2f, 5f).SetEase(Ease.Linear).SetAutoKill(true);
            GetObject(Objects.MainGroup).transform.DOMoveY(600, 5f).SetRelative(true).SetEase(Ease.Linear).SetAutoKill(true);
            GetObject(Objects.Gress).transform.DOScale(1.5f, 5f).SetEase(Ease.Linear).SetAutoKill(true);
            GetObject(Objects.Gress).transform.DOMoveY(-200f, 5f).SetRelative(true).SetEase(Ease.Linear).SetAutoKill(true);

            GetObject(Objects.LeftLeep).transform.DOLocalMove(new Vector3(-200, 50, 0), 5f).SetRelative(true).SetEase(Ease.OutQuart).SetAutoKill(true);
            GetObject(Objects.RightLeep).transform.DOLocalMove(new Vector3(200, 50, 0), 5f).SetRelative(true).SetEase(Ease.OutQuart).SetAutoKill(true);

            GetImage(Images.WhiteOverlay).gameObject.SetActive(true);
            GetImage(Images.WhiteOverlay).DOFade(1f, 1.5f).SetDelay(0.7f).SetEase(Ease.Linear).SetAutoKill(true).OnComplete(() =>
            {
                // StartStep2();
            });
        }, 0.5f);
    }


    //     public void StartStep2()
    //     {
    //         if (_catFaceBlinkRoutine != null)
    //             StopCoroutine(_catFaceBlinkRoutine);
    // 
    //         _currentStep = 2;
    // 
    // 
    //         if (_gachaStep2List.Count > 0)
    //         {
    // 
    //             GachaStep2 firstGachaStep2 = _gachaStep2List[0];
    //             if (firstGachaStep2 != null)
    //             {
    //                 Destroy(firstGachaStep2.gameObject);
    //                 _gachaStep2List.RemoveAt(0);
    //             }
    //         }
    // 
    //         GetObject(Objects.BackgroundGroup).gameObject.SetActive(false);
    //         GachaStep2 gachaStep2 = UIManager.Instance.MakeItemHolder<GachaStep2>(GetObject(Objects.SeowonPopup).transform);
    // 
    //         gachaStep2.Initialize(_currentGachaObject, () =>
    //         {
    //             StartStep3();
    //         });
    // 
    //         _gachaStep2List.Add(gachaStep2);
    // 
    //         GetButton(Buttons.SkipButton).transform.SetAsLastSibling();
    //     }
    // 
    // 
    // 
    // 
    //     public void StartStep3()
    //     {
    //         _currentStep = 3;
    //         GachaStep3 gachaStep3 = UIManager.Instance.MakeItemHolder<GachaStep3>(GetObject(Objects.SeowonPopup).transform);
    // 
    //         gachaStep3.Initialize(_currentGachaObject, () =>
    //         {
    //             if (_gachaObjectList.Count > 0)
    //             {
    //                 int currentIndex = _gachaObjectList.IndexOf(_currentGachaObject);
    //                 if (currentIndex < _gachaObjectList.Count - 1)
    //                 {
    //                     Destroy(gachaStep3.gameObject);
    // 
    //                     _currentGachaObject = _gachaObjectList[currentIndex + 1];
    // 
    //                     if (_currentGachaObject.itemType == GachaObjectType.Character && _currentGachaObject.tier == 1)
    //                     {
    //                         StartStep2();
    //                     }
    //                     else
    //                     {
    //                         StartStep3();
    //                     }
    //                 }
    //                 else
    //                 {
    //                     // If this is the last object, prepare to finish
    //                     _currentGachaObject = null;
    // 
    // 
    //                     HideButton(Buttons.SkipButton);
    // 
    //                     Close();
    // 
    //                     SeowonResultPopupUI seowonResult = UIManager.Instance.ShowPopupUI<SeowonResultPopupUI>();
    //                     seowonResult.Initialize(_gachaObjectList);
    // 
    // 
    //                     foreach (var item in _gachaStep3List)
    //                     {
    //                         Destroy(item.gameObject);
    //                     }
    //                     _gachaStep3List.Clear();
    // 
    // 
    // 
    //                 }
    //             }
    //         });
    //         GetButton(Buttons.SkipButton).transform.SetAsLastSibling();
    // 
    //         _gachaStep3List.Add(gachaStep3);
    //     }

    private void OnSkipButtonClicked()
    {
        HideButton(Buttons.SkipButton);
        UIManager.Instance.ClosePopupUI(this);
        // SeowonResultPopupUI seowonResult = UIManager.Instance.ShowPopupUI<SeowonResultPopupUI>();
        // seowonResult.Initialize(_gachaObjectList);
    }

    //     private IEnumerator ProcessGachaObjectsCoroutine()
    //     {
    //         for (int i = 0; i < _gachaObjectList.Count; i++)
    //         {
    //             
    //         }
    // 
    //         Debug.Log("All gacha objects have been processed.");
    //     }




}
