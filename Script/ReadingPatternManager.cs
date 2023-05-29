using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class ReadingPatternManager : MonoBehaviour {

    public static ReadingPatternManager S = null;

    [System.Serializable]
    public class Block
    {
        public int Num;
        public string Name;
        public int Width;
        public int Height;
        public int Squares;
        public int Limit;
        public int LimitException;
        public int LimitExceptionCount = 0;
        public int[,] Map;
    }

    public class BlockPosition
    {
        public int X;
        public int Y;
        public int Rotate;
        public int Criteria;
        public int ConfirmSqaures;
        public Block Selected;
    }
    
    public List<Block> originalBlockList = new List<Block>();

    public class ColorMap
    {
        public int[,]   map         = null;
    }

    public ColorMap     _ColorMap   = null;
    public GameObject   GAM         = null;

    [Header("Color Window")]
    public Text[]   ColorWindow         = null; // 활성화 되어있는 색상의 개수를 보여주는 Text 배열
    public bool[]   ColorCalcWindow     = null; // 활성화 되어있는 색상을 체크하는 bool 배열
    public Button[] ColorCalcButton     = null; // 활성화 되어있는 색상의 왼편 중앙 레이어 버튼

    // 11개 종류의 블록의 사용 정보를 저장하는 클래스
    [System.Serializable]
    public class WBM
    {
        public bool[] waitBlockCheckList = new bool[16];
    }
    public List<WBM> wbm_List = new List<WBM>(); // 16개의 블록 중 사용될 블록을 체크하는 리스트

    [Tooltip("노, 주, 초, 파, 빨, 갈, 흰, 검, 회, 남, 하늘, 핫핑크, 진남색")]
    public int[]        MatchToSetNum = null; // 각 색상 블록마다 필요한 세트 번호
    public Dropdown[]   MatchDropDown = null; // 각 색상 블록마다 사용되는 드랍다운 UI

    public Text myResultText = null;

    int[,]          prev        = null;
    int[,]          curr        = null;
    public int      colorIndex  = -1;
    public List<Vector2>   posList     = new List<Vector2>(); // 선택된 색상의 블록들의 위치 저장

    int loopCount = 0;

    private int sizeX = 0;
    private int sizeY = 0;

    [Header("ADD BUTTON")]
    public Button BTN_1     = null;
    public Button BTN_2     = null;
    public Button BTN_OK    = null;

    public bool isCheckPixel_5 = false;
    // q를 누를때마다 1. 네칸 탐색, 2. 세칸 탐색, 3. 나머지 탐색 순으로 실행한다.
    // 이때, q를 누를때마다. true(4) false(3) = true(3, 4) false() = true(3) false(4)로 변경된다.
    public bool isCheckPixel_4 = false;
    public bool isCheckPixel_3 = false;

    [Header("LOG VIEW")]
    public GameObject content_      = null;
    public GameObject log_prefab_   = null;

    [Header("TEMP RESULT COLOR")]
    public int Temp_colorIndex      = 0;
    public int Temp_pixelCount      = 0;
    public int Temp_colorSetCount   = 0;

    [Header("PART VIEW")]
    public GameObject PART_VIEW = null;

    public bool isOKCheck = false;

    [System.Serializable]
    public class COLOR_INDEX_SET
    {
        public Color background_Color = Color.white;
        public Color text_Color = Color.white;
    }

    [Header("COLOR INDEX")]
    public COLOR_INDEX_SET[] COLOR_INDEXS = null;

    [Header("LINE MANAGER")]
    public GameObject LINE_MANAGER = null; // 블록과 블록 사이를 이어주는 라인을 모으는 부모 오브젝트

    [Header("WAITING BLOCK WINDOW")]
    public List<GameObject> WB_PANEL_ITEMS = new List<GameObject>();

    //Timer
    [Header("Timer")]
    [SerializeField] private float mTime = 5000.0f;
    [SerializeField] private InputField IF_InputField = null;
    private float eTime = 0.0f;
    private bool isTimeOver = false;

    [Header("Contrast ColorMaps")]
    public List<ColorMap> ConstrastColorMapsList = new List<ColorMap>();
    public  int     ConstrastColorMapNumOf = 0;
    private string  fullName = "";
    private bool    isEqueal = false;
    private string  saveFileName = "";

    [System.Serializable]
    public class LOG
    {
        public int colorIndex = -1;
        public GameObject logObj = null;
        public bool[,] LogHistory_isActive = null;
        public Color[,] LogHistory_myColor = null;
        public string[,] LogHistory_myNumText = null;

        //ADD(라인렌더러 수집)
        public GameObject lineItem = null;

        //(사용한 대기블록 리스트 및 갯수 수집)
        public List<int> WB_NUMOF_LIST = new List<int>();
    }
    public List<LOG> LOGLIST = new List<LOG>();
    public Button LOGTT_Button = null;

    [Header("CalcColorDropDownMenu")]
    public Dropdown CanvasColorDropDownMenu = null;

    [Header("OPTIONS")]
    public Toggle Option_FastMode = null;
    public Toggle Option_MonoMode = null;
    public Toggle Option_PentoMode = null;

    //WBLIST
    private List<int> WaitBlockNumOfList = new List<int>();

    private void Awake()
    {
        S = this;
    }

    /// <summary>
    /// 정해진 색상에 따라 패턴 분석 함수
    /// </summary>
    public void ReadingPatternColorCheck()
    {
        // 맵이 존재하지 않으면 반환
        if (MapManager.S.mapPivot.transform.childCount == 0) return;

        // 타이머 초기화
        eTime = 0.0f;
        isTimeOver = false;

        sizeX = MapManager.S.mapPixel_X; // 맵 X축 크기
        sizeY = MapManager.S.mapPixel_Y; // 맵 Y축 크기

        loopCount = 0; // 무한반복 방지용 루프 카운터 초기화

        // 네칸 탐색을 아직 하지 않았을때 실행, 새로운 맵 정보 생성
        if (isCheckPixel_4 == false)
        {
            _ColorMap = new ColorMap();
        }

        int[] colorIndexCounts = new int[14]; //Default + 8 Color

        // 패턴 읽기
        // 머테리얼 이름으로 색상 위치 및 색상 개수 확인
        for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
        {
            for (int j = 0; j < MapManager.S.mapPixel_X; j++)
            {
                switch (MapManager.S.Pattern_GO[j, i].gameObject.GetComponent<Renderer>().material.name)
                {
                    case "Default_Color (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 0;
                        colorIndexCounts[0]++;
                        break;

                    case "Color_Yellow (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 1;
                        colorIndexCounts[1]++;
                        break;

                    case "Color_Orange (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 2;
                        colorIndexCounts[2]++;
                        break;

                    case "Color_Green (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 3;
                        colorIndexCounts[3]++;
                        break;
                        
                    case "Color_Blue (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 4;
                        colorIndexCounts[4]++;
                        break;

                    case "Color_Red (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 5;
                        colorIndexCounts[5]++;
                        break;

                    case "Color_Brown (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 6;
                        colorIndexCounts[6]++;
                        break;

                    case "Color_White (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 7;
                        colorIndexCounts[7]++;
                        break;

                    case "Color_Black (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 8;
                        colorIndexCounts[8]++;
                        break;

                    case "Color_Gray (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 9;
                        colorIndexCounts[9]++;
                        break;

                    case "Color_Navy (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 10;
                        colorIndexCounts[10]++;
                        break;

                    case "Color_SkyBlue (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 11;
                        colorIndexCounts[11]++;
                        break;

                    case "Color_HotPink (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 12;
                        colorIndexCounts[12]++;
                        break;

                    case "Color_DeepBlue (Instance)":
                        MapManager.S.Pattern_Map[j, i] = 13;
                        colorIndexCounts[13]++;
                        break;
                }
            }
        }


        // 각 맵의 블록 정보를 통합하는 String 변수 생성
        string allData = "";
        for (int i = 0; i < colorIndexCounts.Length; i++)
        {
            switch (i)
            {
                case 0:
                    allData += "Default : " + colorIndexCounts[i] + "\n";
                    break;

                case 1:
                    allData += "YE  : " + colorIndexCounts[i] + "\n";
                    break;

                case 2:
                    allData += "OR  : " + colorIndexCounts[i] + "\n";
                    break;

                case 3:
                    allData += "GN   : " + colorIndexCounts[i] + "\n";
                    break;

                case 4:
                    allData += "BU   : " + colorIndexCounts[i] + "\n";
                    break;

                case 5:
                    allData += "RD    : " + colorIndexCounts[i] + "\n";
                    break;

                case 6:
                    allData += "BR     : " + colorIndexCounts[i] + "\n";
                    break;

                case 7:
                    allData += "WH   : " + colorIndexCounts[i] + "\n";
                    break;

                case 8:
                    allData += "BK   : " + colorIndexCounts[i] + "\n";
                    break;

                case 9:
                    allData += "GY   : " + colorIndexCounts[i] + "\n";
                    break;

                case 10:
                    allData += "NV   : " + colorIndexCounts[i] + "\n";
                    break;

                case 11:
                    allData += "SB   : " + colorIndexCounts[i] + "\n";
                    break;

                case 12:
                    allData += "HP   : " + colorIndexCounts[i] + "\n";
                    break;

                case 13:
                    allData += "DB  : " + colorIndexCounts[i] + "\n";
                    break;
            }
        }
        Debug.Log("블록 개수 : \n" + allData);

        // Q를 한번 클릭할때마다 네칸, 세칸, 기본 탐색을 한다.
        // MapManager의 Pattern_Map은 모든 색상을 나타내는 큰 맵이고, _ColorMap.map은 해당 맵을 읽어 각 색상마다 맵을 저장한다.
        // 네칸 탐색을 아직 하지 않았을때 실행
        //ColorMap Set
        if (isCheckPixel_4 == false)
        {
            // 맵 크기 설정
            _ColorMap.map = new int[MapManager.S.mapPixel_X, MapManager.S.mapPixel_Y];

            for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
            {
                for (int j = 0; j < MapManager.S.mapPixel_X; j++)
                {
                    // 현재 선택된 색상과 동일하면 맵에 입력
                    if (MapManager.S.Pattern_Map[j, i] == colorIndex)
                    {
                        _ColorMap.map[j, i] = colorIndex;
                        //Debug.Log("Color Index Pixel_4 : " + colorIndex);
                    }
                }
            }
        }
        // 세칸 탐색이 없을때
        else if (isCheckPixel_3 == false)
        {
            // 맵 크기 설정
            _ColorMap.map = new int[MapManager.S.mapPixel_X, MapManager.S.mapPixel_Y];

            for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
            {
                for (int j = 0; j < MapManager.S.mapPixel_X; j++)
                {
                    // 현재 선택된 색상과 동일하면 맵에 입력
                    if (MapManager.S.Pattern_Map[j, i] == colorIndex)
                    {
                        _ColorMap.map[j, i] = colorIndex;
                        //Debug.Log("Color Index Pixel_3 : " + colorIndex);
                    }
                }
            }
        }
        // 네칸 or 세칸 탐색이 있을때
        else
        {
            for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
            {
                for (int j = 0; j < MapManager.S.mapPixel_X; j++)
                {
                    // 현재 선택된 색상과 동일하면 맵에 입력
                    if (_ColorMap.map[j, i] == 99)
                    {
                        _ColorMap.map[j, i] = colorIndex;
                        //Debug.Log("Color Index Normal : " + colorIndex);
                    }
                }
            }
        }

        //Reading Pattern Algorithm ========================================
        ReadingPatternAlgorithm();
    }
    
    /// <summary>
    /// 패턴 인식 알고리즘 함수
    /// </summary>
    public void ReadingPatternAlgorithm()
    {
        // 처음 q를 실행한 경우
        if(isCheckPixel_4 == false && isCheckPixel_3 == false)
        {
            // 이전에 사용하던 GAM오브젝트가 존재하면 실행
            if (GAM != null)
            {
                // 오브젝트 활성화
                for (int i = 0; i < GAM.transform.childCount; i++)
                {
                    // 색상 랜덤
                    Color newColor = new Color(Random.value, Random.value, Random.value);

                    // 1000~16000까지 오브젝트에 들어있는 자식 블록 정보 호출 Pixel -> Marker
                    for (int j = 0; j < GAM.transform.GetChild(i).transform.childCount; j++)
                    {
                        // 마커 표시 오브젝트 비활성화
                        GAM.transform.GetChild(i).transform.GetChild(j).gameObject.transform.GetChild(1).gameObject.SetActive(false);
                    }
                }

                // 모든 픽셀 오브젝트 부모를 mapPivot으로 복구
                for (int i = 0; i < GAM.transform.childCount; i++)
                {
                    for (int j = GAM.transform.GetChild(i).childCount - 1; j >= 0; j--)
                    {
                        GAM.transform.GetChild(i).transform.GetChild(j).transform.parent = MapManager.S.mapPivot.transform;
                    }
                }

                // GAM 제거
                Destroy(GAM);
            }

            // 새로운 GAM 생성
            GAM = new GameObject();
            GAM.gameObject.name = "GAM";
        }
        // Debug.Log("Reading Pattern Algorithm Start..*");

        // 정답 string 생성
        myResultText.text = "";
        string result = "";

        // 선택된 색상이 없으면 반환
        if (colorIndex == -1) return;

        // 각 색상이 활성화 되어 있는가 확인
        if (ColorCalcWindow[colorIndex - 1])
        {
            posList.Clear(); // 위치 값 초기화

            //검증완료======>
            prev = new int[MapManager.S.mapPixel_X, MapManager.S.mapPixel_Y];
            curr = new int[MapManager.S.mapPixel_X, MapManager.S.mapPixel_Y];

            prev = _ColorMap.map;
            curr = prev;

            // 첫번째 q
            if (isCheckPixel_4 == false && isCheckPixel_3 == false)
            {
                wbm_List.Clear(); // 사용된 블록 정보 초기화

                // 각 색상의 세트 수만큼 16개의 블록의 사용 정보를 저장할 클래스 생성
                // 노란색이 3개의 세트가 필요하다면 3개의 WBM클래스를 Add하고 48개의 블록을 사용하는 것이다.
                for (int i = 0; i < MatchToSetNum[colorIndex]; i++)
                {
                    wbm_List.Add(new WBM());
                }
            }
            // 이 후 q 탐색
            else
            {
                // 나머지 탐색
                if (isCheckPixel_4 == false && isCheckPixel_3)
                {
                    // 마법봉 작업
                    // (1) 점수를 계산합니다. 인접한 ColorIndex가 있을시, 해당칸에 1점씩 더해 부여합니다. 최대 4로 (상/하/좌/우)의 픽셀을 계산합니다.

                    // 맵 크기만큼 calcMap 할당
                    int[,] calcMap = new int[MapManager.S.mapPixel_X, MapManager.S.mapPixel_Y];

                    // AM1 위치 값 리스트
                    List<Vector2> MonoVecList = new List<Vector2>();

                    // 맵 돌아보기
                    // 각 블록 주변에 얼마나 블록이 있는지 확인하여 존재하는 블록 수를 저장
                    // 빈 블록 확인 및 연결된 블록이 없는지 혼자 남은 블록(AM1) 확인
                    for (int y = 0; y < sizeY; y++)
                    {
                        for (int x = 0; x < sizeX; x++)
                        {
                            // 필요로하는 색상을 찾은 경우
                            if (_ColorMap.map[x, y] == colorIndex)
                            {
                                int calcScore = 0;

                                // 맵 밖을 나가지 않는 한에서 같인 색상인지 확인
                                if (y + 1 < sizeY && _ColorMap.map[x, y + 1] == colorIndex) // 상
                                    calcScore++;
                                if (y - 1 >= 0 && _ColorMap.map[x, y - 1] == colorIndex)    // 하
                                    calcScore++;
                                if (x + 1 < sizeX && _ColorMap.map[x + 1, y] == colorIndex) // 우
                                    calcScore++;
                                if (x - 1 >= 0 && _ColorMap.map[x - 1, y] == colorIndex)    // 좌
                                    calcScore++;

                                calcMap[x, y] = calcScore; // 해당 위치와 연결된 블록 수 입력

                                // 연결된 블록이 없으면 mono 블록 설정
                                if (calcScore == 0 && Option_MonoMode.isOn)
                                {
                                    MonoVecList.Add(new Vector2(x, y));
                                }
                            }
                            else
                            {
                                // 동일한 색상의 블록이 아니면 빈 블록으로 처리
                                calcMap[x, y] = -1;
                            }
                        }
                    }

                    //AM1 걸러내기
                    int remainMonoCount = 0;
                    // 선택된 색상의 세트 수만큼 반복
                    // AM1블록 중 사용 가능한 블록 확인
                    for (int i = 0; i < MatchToSetNum[colorIndex]; i++)
                    {
                        if (wbm_List[i].waitBlockCheckList[0] == false)
                        {
                            remainMonoCount++;
                        }
                    }

                    // --------------------------------------------------
                    // 각 블록 패턴 생성
                    // AM1이 부족하지 않으면 실행
                    if (MonoVecList.Count <= remainMonoCount)
                    {
                        #region AM1
                        // 인식된 블록만큼 반복
                        // AM1 블록이 사용되어야 할 위치에 AM1 활성화
                        for (int i = 0; i < MonoVecList.Count; i++)
                        {
                            // 각 블록을 위치 시키고 1000 입력
                            _ColorMap.map[(int)MonoVecList[i].x, (int)MonoVecList[i].y] = 1000;

                            // 선택된 색상의 세트만큼 반복
                            for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                            {
                                // 세트의 AM1블록이 사용되지 않았으면 사용으로 전환
                                if (wbm_List[j].waitBlockCheckList[0] == false)
                                {
                                    wbm_List[j].waitBlockCheckList[0] = true;
                                    break;
                                }
                            }
                        }

                        // AM1 블록 사용 가능한 개수 감소
                        remainMonoCount -= MonoVecList.Count;
                        //Debug.Log("남은 모노미노 갯수 : " + remainMonoCount);
                        #endregion

                        //D1 걸러내기
                        #region D1
                        List<Vector2> D1_Map = new List<Vector2>();
                        List<Vector2> Avaliable_D1_Map = new List<Vector2>();


                        // 블록의 연결된 블록이 1개인 경우 D1 리스트에 추가
                        for (int y = 0; y < sizeY; y++)
                        {
                            for (int x = 0; x < sizeX; x++)
                            {
                                // 선택된 블록의 연결된 방향이 1인 경우
                                if (calcMap[x, y] == 1)
                                {
                                    // D1 리스트에 추가
                                    D1_Map.Add(new Vector2(x, y));
                                }
                            }
                        }

                        int d1Count = 0;
                        int remainD1Count = 0;

                        //Debug.Log("D1 예상 갯수 : " + D1_Map.Count);
                        // D1_Map리스트에 들어있는 각 D1을 확인하며 두 D1 원소들이 서로 옆에 있으면 D1 블록으로 설정
                        for (int i = 0; i < D1_Map.Count; i++)
                        {
                            for (int j = 0; j < D1_Map.Count; j++)
                            {
                                // 동일한 D1 블록을 확인하면 넘어가기
                                if (i == j) continue;

                                // 대각선이 아닌 수직/수평 관계에서 두 D1이 동일하면 D1으로 입력
                                if ((Mathf.Abs(D1_Map[i].x - D1_Map[j].x) == 1 && Mathf.Abs(D1_Map[i].y - D1_Map[j].y) == 0) ||
                                    (Mathf.Abs(D1_Map[i].y - D1_Map[j].y) == 1 && Mathf.Abs(D1_Map[i].x - D1_Map[j].x) == 0))
                                {
                                    // 리스트의 2자리를 하나의 D1으로 입력
                                    Avaliable_D1_Map.Add(D1_Map[i]);
                                    Avaliable_D1_Map.Add(D1_Map[j]);

                                    // D1 추가
                                    d1Count++;
                                }
                            }
                        }
                        // 2블록으로 D1을 구성하기에 절반으로 나누기
                        d1Count = (int)(d1Count * 0.5f);

                        // 사용가능한 D1 블록 수 확인
                        for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                        {
                            if (wbm_List[j].waitBlockCheckList[1] == false)
                            {
                                remainD1Count++;
                            }
                        }

                        Avaliable_D1_Map = Avaliable_D1_Map.Distinct().ToList();

                        //Debug.Log("발견된 D1 갯수 : " + d1Count + " 사용가능한 D1 갯수 : " + remainD1Count);
                        // D1의 필요 개수가 남아있는 개수보다 작거나 같을때 실행
                        // 세트에서 사용할 수 있는 D1 수와 비교하여 사용한 D1 블록 수를 확인 후 입력
                        if (d1Count <= remainD1Count)
                        {
                            // 할당 가능한 D1 개수 만큼 반복
                            for (int i = 0; i < Avaliable_D1_Map.Count; i++)
                            {
                                //Debug.Log((int)Avaliable_D1_Map[i].x + ":" + (int)Avaliable_D1_Map[i].y);
                                // D1 리스트 위치를 2000으로 선언
                                _ColorMap.map[(int)Avaliable_D1_Map[i].x, (int)Avaliable_D1_Map[i].y] = 2000;

                                for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                                {
                                    if (wbm_List[j].waitBlockCheckList[1] == false)
                                    {
                                        wbm_List[j].waitBlockCheckList[1] = true;
                                        break;
                                    }
                                }
                            }

                            remainD1Count -= d1Count;
                        }
                        // 사용가능한 D1이 남아있지 않으면 AM1으로 채울 수 있는지 확인하는 구문 실행
                        else
                        {
                            int remainD1_MonoCount = d1Count - remainD1Count;
                            //Debug.Log("사용가능한 모노미노 갯수 : " + remainMonoCount + " 필요한 D1(모노미노) 갯수 : " + remainD1_MonoCount * 2);

                            remainD1_MonoCount *= 2;

                            // 남은 D1의 1칸 블록들이 남은 AM1보다 적을때 실행
                            if (remainD1_MonoCount < remainMonoCount && Option_MonoMode.isOn)
                            {
                                for (int i = 0; i < Avaliable_D1_Map.Count; i++)
                                {
                                    // 남은 D1 블록이 존재하면 AM1으로 변경하여 저장
                                    // D1이 될 수 없어진 D1 블록들이 남으면 사용가능한 AM1이 존재하면 그냥 AM1으로 돌려버린다.
                                    if (remainD1_MonoCount > 0 && Option_MonoMode.isOn)
                                    {
                                        //Debug.Log((int)Avaliable_D1_Map[i].x + "," + (int)Avaliable_D1_Map[i].y);
                                        _ColorMap.map[(int)Avaliable_D1_Map[i].x, (int)Avaliable_D1_Map[i].y] = 1000;

                                        for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                                        {
                                            if (wbm_List[j].waitBlockCheckList[0] == false)
                                            {
                                                wbm_List[j].waitBlockCheckList[0] = true;
                                                break;
                                            }
                                        }

                                        remainD1_MonoCount--;
                                    }
                                    // AM1이 남아있지 않으면 일단 D1으로 유지
                                    else
                                    {
                                        //Debug.Log((int)Avaliable_D1_Map[i].x + "," + (int)Avaliable_D1_Map[i].y);
                                        _ColorMap.map[(int)Avaliable_D1_Map[i].x, (int)Avaliable_D1_Map[i].y] = 2000;

                                        for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                                        {
                                            if (wbm_List[j].waitBlockCheckList[1] == false)
                                            {
                                                wbm_List[j].waitBlockCheckList[1] = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                ResetButtons();
                                myResultText.text = "세트 수 부족";
                                return;
                            }
                        }
                        #endregion

                        //Debug.Log("사용가능한 AM1 갯수 : " + remainMonoCount);
                        //Debug.Log("사용가능한 D1  갯수 : " + remainD1Count);


                        #region TR
                        //TR1 ~ TR2 걸러내기
                        // TR1 : ㅡ
                        // TR2 : ㄱ
                        List<Vector2> TR_Map = new List<Vector2>();
                        List<Vector2> Avaliable_TR1_Map = new List<Vector2>();
                        List<Vector2> Avaliable_TR2_Map = new List<Vector2>();


                        // 상하좌우 어디든 2개의 블록과 연결되어 있으면 TR리스트에 입력
                        for (int y = 0; y < sizeY; y++)
                        {
                            for (int x = 0; x < sizeX; x++)
                            {
                                if (calcMap[x, y] == 2)
                                {
                                    TR_Map.Add(new Vector2(x, y));
                                }
                            }
                        }

                        List<Vector2> tempList = new List<Vector2>();

                        // TR_Map만큼 반복
                        for (int i = 0; i < TR_Map.Count; i++)
                        {
                            tempList.Clear();

                            int x = (int)TR_Map[i].x;
                            int y = (int)TR_Map[i].y;

                            tempList.Add(new Vector2(x, y));

                            // 3개의 블록이 합쳐야 TR블록들이 가능하다.
                            // oneScore는 전방향을 확인하여 다른 연결지점이 없는 블록들만 확인하여 2개 이상 연결되면 TR로 확인한다.
                            int oneScore = 0;
                            // 맵 밖을 나가지 않는 한에서 TR_Map 위치별 상하좌우 확인
                            if (y + 1 < sizeY && calcMap[x, y + 1] == 1) // 상
                            {
                                tempList.Add(new Vector2(x, y + 1));
                                oneScore++;
                            }
                            if (y - 1 >= 0 && calcMap[x, y - 1] == 1) // 하
                            {
                                tempList.Add(new Vector2(x, y - 1));
                                oneScore++;
                            }
                            if (x + 1 < sizeX && calcMap[x + 1, y] == 1) // 우
                            {
                                tempList.Add(new Vector2(x + 1, y));
                                oneScore++;
                            }
                            if (x - 1 >= 0 && calcMap[x - 1, y] == 1) // 좌
                            {
                                tempList.Add(new Vector2(x - 1, y));
                                oneScore++;
                            }

                            // 2개 이상 연결되면 TR로 입력
                            // TR을 이루는 3개의 블록 중 중간 블록과 첫 블록이 수직 혹은 수평이면 TR1 리스트에 등록
                            // 수직 혹은 수평이 아니면 TR2 리스트에 등록
                            if (oneScore == 2)
                            {
                                // TR을 이루는 3개의 블록 중 중간 블록과 첫 블록이 수직 혹은 수평이면 TR1 리스트에 등록
                                if ((tempList[tempList.Count - 1].x == tempList[tempList.Count - 2].x) ||
                                   (tempList[tempList.Count - 1].y == tempList[tempList.Count - 2].y))
                                {
                                    Avaliable_TR1_Map.AddRange(tempList);
                                }
                                // 수직 혹은 수평이 아니면 TR2 리스트에 등록
                                else
                                {
                                    Avaliable_TR2_Map.AddRange(tempList);
                                }
                            }
                        }

                        Avaliable_TR1_Map = Avaliable_TR1_Map.Distinct().ToList();
                        Avaliable_TR2_Map = Avaliable_TR2_Map.Distinct().ToList();
                        //Debug.Log("검색된 TR1 갯수 : " + Avaliable_TR1_Map.Count / 3);
                        //Debug.Log("검색된 TR2 갯수 : " + Avaliable_TR2_Map.Count / 3);

                        //TR1걸러내기 
                        int remainTR1Count = 0;

                        // TR1 세트 수 확인
                        for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                        {
                            if (wbm_List[j].waitBlockCheckList[2] == false)
                            {
                                remainTR1Count++;
                            }
                        }

                        // TR1 생성, 필요로하는 TR1보다 사용 가능한 TR1이 많은 경우 실행
                        if (Avaliable_TR1_Map.Count / 3 <= remainTR1Count)
                        {
                            // 남아있는 TR1 카운트 확인
                            remainTR1Count -= Avaliable_TR1_Map.Count / 3;

                            for (int i = 0; i < Avaliable_TR1_Map.Count; i++)
                            {
                                _ColorMap.map[(int)Avaliable_TR1_Map[i].x, (int)Avaliable_TR1_Map[i].y] = 3000;

                                for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                                {
                                    if (wbm_List[j].waitBlockCheckList[2] == false)
                                    {
                                        wbm_List[j].waitBlockCheckList[2] = true;
                                        break;
                                    }
                                }
                            }
                        }
                        // TR1이 부족하면 D1으로 대체 가능한지 확인하는 구문 실행 
                        else
                        {
                            int remainTR1MAPCount = (Avaliable_TR1_Map.Count / 3 - remainTR1Count);
                            Debug.Log("부족한 TR1 갯수 : " + remainTR1MAPCount + " 모노미노 갯수 : " + remainMonoCount + " D1 갯수 : " + remainD1Count);
                            
                            // 남은 TR1을 AM1과 D1으로 교체할 수 있는가?
                            if (Option_MonoMode.isOn &&
                                (remainMonoCount >= remainTR1MAPCount) &&
                                (remainD1Count >= remainMonoCount))
                            {
                                int poolCount = 0;
                                int poolMCount = 3;

                                int ttt = remainTR1MAPCount * 3; // TR1이 있어야 했을 블록의 수
                                for (int i = 0; i < Avaliable_TR1_Map.Count; i++)
                                {
                                    if (ttt > 0) // 아직 채워야할 블록이 남으면 실행
                                    {
                                        if (poolCount < 2) // 2개의 블록으 D1으로 등록
                                        {
                                            _ColorMap.map[(int)Avaliable_TR1_Map[i].x, (int)Avaliable_TR1_Map[i].y] = 2000;

                                            for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                                            {
                                                if (wbm_List[j].waitBlockCheckList[1] == false)
                                                {
                                                    wbm_List[j].waitBlockCheckList[1] = true;
                                                    break;
                                                }
                                            }
                                        }
                                        else // 3번째 블록을 AM1으로 등록
                                        {
                                            _ColorMap.map[(int)Avaliable_TR1_Map[i].x, (int)Avaliable_TR1_Map[i].y] = 1000;

                                            for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                                            {
                                                if (wbm_List[j].waitBlockCheckList[0] == false)
                                                {
                                                    wbm_List[j].waitBlockCheckList[0] = true;
                                                    break;
                                                }
                                            }
                                        }


                                        if (poolCount == poolMCount) 
                                            poolCount = 0;

                                        poolCount++;
                                        ttt--;
                                    }
                                    else
                                    {
                                        _ColorMap.map[(int)Avaliable_TR1_Map[i].x, (int)Avaliable_TR1_Map[i].y] = 3000;

                                        for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                                        {
                                            if (wbm_List[j].waitBlockCheckList[2] == false)
                                            {
                                                wbm_List[j].waitBlockCheckList[2] = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            // 교체가 불가능하면 세트 수 부족 실행
                            else
                            {
                                ResetButtons();
                                myResultText.text = "세트 수 부족";
                                return;
                            }
                        }

                        // TR2걸러내기
                        int remainTR2Count = 0;

                        // TR2 사용 가능한 세트 확인
                        for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                        {
                            if (wbm_List[j].waitBlockCheckList[3] == false)
                            {
                                remainTR2Count++;
                            }
                        }

                        // 사용가능한 TR2가 필요한 TR2 개수보다 많으면 실행
                        if (Avaliable_TR2_Map.Count / 3 <= remainTR2Count)
                        {
                            // 남은 TR2 개수 저장
                            remainTR2Count -= Avaliable_TR2_Map.Count / 3;

                            // TR2 활성화
                            for (int i = 0; i < Avaliable_TR2_Map.Count; i++)
                            {
                                // 4000으로 저장
                                _ColorMap.map[(int)Avaliable_TR2_Map[i].x, (int)Avaliable_TR2_Map[i].y] = 4000;

                                // TR2 블록 활성화
                                for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                                {
                                    if (wbm_List[j].waitBlockCheckList[3] == false)
                                    {
                                        wbm_List[j].waitBlockCheckList[3] = true;
                                        break;
                                    }
                                }
                            }
                        }
                        // 그렇지 않으면 남은 개수를 D1과 AM1으로 변경 가능한지 확인
                        else
                        {
                            int remainTR2MAPCount = (Avaliable_TR2_Map.Count / 3 - remainTR2Count);
                            // Debug.Log("부족한 TR1 갯수 : " + remainTR2MAPCount + " 모노미노 갯수 : " + remainMonoCount + " D1 갯수 : " + remainD1Count);

                            // 필요한 AM1 개수가 충족하면 TR1을 AM1으로 변경
                            if (Option_MonoMode.isOn &&
                                (remainMonoCount >= remainTR2MAPCount) &&
                                (remainD1Count >= remainMonoCount))
                            {
                                int poolCount = 0;
                                int poolMCount = 3;

                                int ttt = remainTR2MAPCount * 3; // TR2로 사용되던 블록 개수 확인

                                // TR2 개수만큼 반복
                                for (int i = 0; i < Avaliable_TR2_Map.Count; i++)
                                {
                                    if (ttt > 0) // 남은 블록이 있으면 반복
                                    {
                                        if (poolCount < 2) // 2칸을 D1으로 연결
                                        {
                                            //Debug.Log(poolCount);
                                            _ColorMap.map[(int)Avaliable_TR2_Map[i].x, (int)Avaliable_TR2_Map[i].y] = 2000;

                                            for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                                            {
                                                if (wbm_List[j].waitBlockCheckList[1] == false)
                                                {
                                                    wbm_List[j].waitBlockCheckList[1] = true;
                                                    break;
                                                }
                                            }
                                        }
                                        // 1칸을 AM1으로 입력
                                        else
                                        {
                                            //Debug.Log(poolCount);
                                            _ColorMap.map[(int)Avaliable_TR2_Map[i].x, (int)Avaliable_TR2_Map[i].y] = 1000;

                                            for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                                            {
                                                if (wbm_List[j].waitBlockCheckList[0] == false)
                                                {
                                                    wbm_List[j].waitBlockCheckList[0] = true;
                                                    break;
                                                }
                                            }
                                        }

                                        if (poolCount == poolMCount)
                                            poolCount = 0;

                                        poolCount++;
                                        ttt--;
                                    }
                                    // 남은 블록이 없는데 반복하면 TR2로 입력
                                    else
                                    {
                                        _ColorMap.map[(int)Avaliable_TR2_Map[i].x, (int)Avaliable_TR2_Map[i].y] = 4000;

                                        for (int j = 0; j < MatchToSetNum[colorIndex]; j++)
                                        {
                                            if (wbm_List[j].waitBlockCheckList[3] == false)
                                            {
                                                wbm_List[j].waitBlockCheckList[3] = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            // 개수가 부족하면 실행
                            else
                            {
                                ResetButtons();
                                myResultText.text = "세트 수 부족";
                                return;
                            }
                        }
                    }
                    // AM1이 애초에 부족하면 세트수가 모자란 것으로 확인하여 실행
                    // 세트 수 부족
                    else
                    {
                        ResetButtons();
                        myResultText.text = "세트 수 부족";
                        return;
                    }
                    #endregion
                }
            }

            int allCount = 0; // 찾은 색상만큼 카운트
            for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
            {
                for (int j = 0; j < MapManager.S.mapPixel_X; j++)
                {
                    // 현재 인식한 색상만 찾기
                    if (_ColorMap.map[j, i] == colorIndex)
                    {
                        // 블록 카운트 ++
                        allCount++;

                        // 아직 탐색을 안했을때 실행
                        if (isCheckPixel_4 == false)
                        {
                            // 실제 3D Pixel 오브젝트가 네칸 탐색용이 되어 있으면 실행
                            if (MapManager.S.Pattern_GO[j, i].gameObject.GetComponent<Pixel>().isBranch == 4)
                            {
                                // 리스트 등록
                                posList.Add(new Vector2(j, i));
                            }
                            else
                            {
                                // 그렇지 않으면 탐색에서 제외
                                _ColorMap.map[j, i] = 99;
                            }
                        }
                        //*문제소지
                        // 네칸 탐지를 했으며 세칸 탐색가 아직 일때, 세칸 탐색
                        else if (isCheckPixel_4 && isCheckPixel_3 == false)
                        {
                            // 세칸 탐색용 브랜치이면 실행
                            if (MapManager.S.Pattern_GO[j, i].gameObject.GetComponent<Pixel>().isBranch == 3)
                            {
                                // 리스트 등록
                                posList.Add(new Vector2(j, i));
                            }
                            // 그렇지 않은 경우 탐색에서 제외
                            else
                            {
                                _ColorMap.map[j, i] = 99;
                            }
                        }
                        // 이외 남은 블록 탐색
                        else
                        {
                            // 특정 탐색이 없는 경우
                            if (MapManager.S.Pattern_GO[j, i].gameObject.GetComponent<Pixel>().isBranch == -1)
                            {
                                // 리스트 입력
                                posList.Add(new Vector2(j, i));
                            }
                            // 그렇지 않은 경우 탐색에서 제외
                            else
                            {
                                _ColorMap.map[j, i] = 99;
                            }
                        }
                    }
                }
            }
            //for (int i = 0; i < posList.Count; i++)
            //{
            //    Debug.Log(ColorMaps[colorIndex].map[(int)posList[i].x, (int)posList[i].y]);
            //}

            //세트 수 예외 검사
            bool isNo = false;
            int comapre = (Option_PentoMode.isOn) ? 62 : 37;
            //Debug.Log(allCount + "<>" + MatchToSetNum[m]);
            // 보유 세트 수보다 필요한 블록 수가 많을때, 예외처리
            if (allCount > MatchToSetNum[colorIndex] * comapre)
            {
                switch (colorIndex)
                {
                    case 1:
                        result += " YE";
                        break;

                    case 2:
                        result += " OR";
                        break;

                    case 3:
                        result += " GN";
                        break;

                    case 4:
                        result += " BU";
                        break;

                    case 5:
                        result += " RD";
                        break;

                    case 6:
                        result += " BR";
                        break;

                    case 7:
                        result += " WH";
                        break;

                    case 8:
                        result += " BK";
                        break;

                    case 9:
                        result += " GY";
                        break;

                    case 10:
                        result += " NV";
                        break;

                    case 11:
                        result += " SB";
                        break;

                    case 12:
                        result += " HP";
                        break;

                    case 13:
                        result += " DB";
                        break;
                }

                result += " : 세트 수 체크";
                myResultText.text = result;
                return;
            }

            // 리스트가 비어있으면 실행
            if (posList.Count == 0)
            {
                // 네칸 탐색을 아직 하지 않았을때 실행
                if (isCheckPixel_4 == false)
                {
                    BTN_1.interactable = false;
                    BTN_2.interactable = true;

                    isCheckPixel_4 = true;

                    result = "4픽셀 - YES";

                    myResultText.text = result;
                }
                else if (isCheckPixel_4 && isCheckPixel_3 == false)
                {
                    BTN_1.interactable = false;
                    BTN_2.interactable = true;

                    isCheckPixel_3 = true;

                    result = "3픽셀 - YES";

                    myResultText.text = result;
                }
                else
                {
                    BTN_1.interactable = false;
                    BTN_2.interactable = false;
                    BTN_OK.gameObject.SetActive(true);

                    isCheckPixel_4 = false;
                    isCheckPixel_3 = false;

                    switch (colorIndex)
                    {
                        case 1:
                            result += "YE";
                            break;

                        case 2:
                            result += "OR";
                            break;

                        case 3:
                            result += "GN";
                            break;

                        case 4:
                            result += "BU";
                            break;

                        case 5:
                            result += "RD";
                            break;

                        case 6:
                            result += "BR";
                            break;

                        case 7:
                            result += "WH";
                            break;

                        case 8:
                            result += "BK";
                            break;

                        case 9:
                            result += "GY";
                            break;

                        case 10:
                            result += "NV";
                            break;

                        case 11:
                            result += "SB";
                            break;

                        case 12:
                            result += "HP";
                            break;

                        case 13:
                            result += "DB";
                            break;
                    }

                    result += " YES";

                    myResultText.text = result;
                }

                Temp_colorIndex = colorIndex;
                Temp_colorSetCount = MatchToSetNum[colorIndex];
                Temp_pixelCount = int.Parse(ColorWindow[colorIndex - 1].text);

                isNo = false;
            }
            // 리스트에 탐색할 목록이 있으면 실행
            else
            {
                int blockCount = 0;
                 
                // 네칸 탐색을 아직 하지 않았을때 실행
                if (isCheckPixel_4 == false)
                {
                    // 네칸 탐색 중 문구 입력
                    result += "4픽셀 ";
                }
                else if (isCheckPixel_4 && isCheckPixel_3 == false)
                {
                    // 세칸 탐색 중 문구 입력
                    result += "3픽셀 ";
                }

                
                List<int> PosList_OrderByList = new List<int>();    // 선택된 블록의 위치 정렬
                List<int> PosList_OrderByList2 = new List<int>();   // 선택된 블록의 위치 랜덤 정렬

                // 탐색할 블록 영역 순서대로 입력
                for (int i = 0; i < posList.Count; i++)
                {
                    PosList_OrderByList.Add(i);
                }
                // 탐색할 블록 영역 랜덤 순서로 재정렬
                for (int i = 0; i < posList.Count; i++)
                {
                    int rVal = Random.Range(0, PosList_OrderByList.Count);
                    PosList_OrderByList2.Add(rVal);
                    PosList_OrderByList.RemoveAt(rVal);
                }


                while (true)
                {
                    // 랜덤으로 정렬된 블록 위치의 첫 원소부터 패턴 확인
                    if (ReadingPatternCheck(PosList_OrderByList2[blockCount]))
                    {
                        result += "YES";

                        // 네칸 탐색을 아직 하지 않았을때 실행
                        if (isCheckPixel_4 == false)
                        {
                            BTN_1.interactable = false;
                            BTN_2.interactable = true;

                            isCheckPixel_4 = true;
                        }
                        // 네칸 탐색을 했지만, 아직 세칸 탐색을 하지 않았을때 실행
                        else if (isCheckPixel_4 && isCheckPixel_3 == false)
                        {
                            BTN_1.interactable = false;
                            BTN_2.interactable = true;

                            isCheckPixel_3 = true;
                        }
                        // 모두 끝났을때 실행
                        else
                        {
                            BTN_1.interactable = false;
                            BTN_2.interactable = false;
                            BTN_OK.gameObject.SetActive(true);

                            isCheckPixel_4 = false;

                            Temp_colorIndex = colorIndex;
                            Temp_colorSetCount = MatchToSetNum[colorIndex];
                            Temp_pixelCount = int.Parse(ColorWindow[colorIndex - 1].text);
                        }

                        //비교후 카운팅 시도(경우의 수 핵심코드) - 칼라값으로하면안됨 TODO
                        if (isCheckPixel_4 == false && isCheckPixel_3)
                        {
                            isEqueal = false;

                            for (int i = 0; i < ConstrastColorMapsList.Count; i++)
                            {
                                if (ConstrastColorMapsList[i].map.OfType<int>().SequenceEqual(_ColorMap.map.OfType<int>()))
                                {
                                    isEqueal = true;
                                    break;
                                }
                                //else
                                //{
                                //    string tt = "";
                                //    foreach (var item in ColorMaps[colorIndex].map)
                                //    {
                                //        tt += item;
                                //    }

                                //    Debug.Log(tt);

                                //    tt = "";

                                //    foreach (var item in ConstrastColorMapsList[i].map)
                                //    {
                                //        tt += item;
                                //    }

                                //    Debug.Log(tt);
                                //}
                            }
                            //Debug.Log(isCheckPixel_4 + "_" + isCheckPixel_3);

                            if (isTimeOver == false && isEqueal == false)
                            {
                                ConstrastColorMapNumOf++;
                                //Debug.Log("ConstrastColorMapNumOf : " + ConstrastColorMapNumOf);

                                ColorMap newColorMap = new ColorMap();
                                newColorMap.map = new int[_ColorMap.map.GetLength(0), _ColorMap.map.GetLength(1)];

                                for (int i = 0; i < _ColorMap.map.GetLength(0); i++)
                                {
                                    for (int j = 0; j < _ColorMap.map.GetLength(1); j++)
                                    {
                                        newColorMap.map[i, j] = _ColorMap.map[i, j];
                                    }
                                }

                                string alldata = "";
                                foreach (var item in newColorMap.map)
                                {
                                    alldata += item;
                                }

                                //Debug.Log("저장 : " + alldata);
                                ConstrastColorMapsList.Add(newColorMap);
                            }
                        }
                        break;
                    }
                    // PosList_OrderByList2를 순차적으로 검사하며 선택된 원소가 맞지 않으면 다음 원소로 탐색한다.
                    else
                    {
                        // blockCount번째 원소가 블록을 놓을 수 없다면 다음 번호로 실행
                        blockCount++;
                        // 모든 블록 수를 다 탐색해도 정답을 못 찾으면 알고리즘 실패
                        if (blockCount >= posList.Count)
                        {
                            switch (colorIndex)
                            {
                                case 1:
                                    result += "YE";
                                    break;

                                case 2:
                                    result += "OR";
                                    break;

                                case 3:
                                    result += "GN";
                                    break;

                                case 4:
                                    result += "BU";
                                    break;

                                case 5:
                                    result += "RD";
                                    break;

                                case 6:
                                    result += "BR";
                                    break;

                                case 7:
                                    result += "WH";
                                    break;

                                case 8:
                                    result += "BK";
                                    break;

                                case 9:
                                    result += "GY";
                                    break;

                                case 10:
                                    result += "NV";
                                    break;

                                case 11:
                                    result += "SB";
                                    break;

                                case 12:
                                    result += "HP";
                                    break;

                                case 13:
                                    result += "DB";
                                    break;
                            }

                            isNo = true;
                            result += " NO";
                            ResetButtons();
                            BTN_1.interactable = false;
                            BTN_2.interactable = false;
                            break;
                        }
                        //Debug.Log("Point : " + posList[blockCount]);
                    }
                }
            }

            //***
            string color = "";
            string prevResult = result;

            switch (colorIndex)
            {
                case 1:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += "YE";
                        result += " " + prevResult;
                    }

                    color = "YELLOW";
                    break;

                case 2:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += "OR";
                        result += " " + prevResult;
                    }

                    color = "ORANGE";
                    break;

                case 3:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += "GN";
                        result += " " + prevResult;
                    }

                    color = "GREEN";
                    break;

                case 4:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += " BU";
                        result += " " + prevResult;
                    }

                    color = "BLUE";
                    break;

                case 5:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += " RD";
                        result += " " + prevResult;
                    }

                    color = "RED";
                    break;

                case 6:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += " BR";
                        result += " " + prevResult;
                    }

                    color = "BROWN";
                    break;

                case 7:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += " WH";
                        result += " " + prevResult;
                    }

                    color = "WHITE";
                    break;

                case 8:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += " BK";
                        result += " " + prevResult;
                    }

                    color = "BLACK";
                    break;

                case 9:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += " GY";
                        result += " " + prevResult;
                    }

                    color = "GRAY";
                    break;

                case 10:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += " NV";
                        result += " " + prevResult;
                    }

                    color = "NAVY";
                    break;

                case 11:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += " SB";
                        result += " " + prevResult;
                    }

                    color = "SKYBLUE";
                    break;

                case 12:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += " HP";
                        result += " " + prevResult;
                    }

                    color = "HOTPINK";
                    break;

                case 13:
                    if (isCheckPixel_4 == false && isCheckPixel_3)
                    {
                        result = "";
                        result += " DB";
                        result += "" + prevResult;
                    }

                    color = "DEEPBLUE";
                    break;
            }

            int limit = (Option_PentoMode.isOn) ? 16000 + MatchToSetNum[colorIndex] : 11000 + MatchToSetNum[colorIndex];
            for (int idx = 1000; idx < limit; idx++)
            {
                // 네칸 탐색을 아직 하지 않았을때 실행
                if (isCheckPixel_4 == false)
                {
                    string allData = "isCheckPixel_4 : " + isCheckPixel_4 + "idx : " + idx + " \n";
                    GameObject part = new GameObject();
                    part.transform.parent = GAM.transform;

                    part.name = idx.ToString();

                    for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
                    {
                        for (int j = 0; j < MapManager.S.mapPixel_X; j++)
                        {
                            if (curr[j, i] == idx)
                            {
                                allData += "[" + j + "," + i + "] \n";
                                MapManager.S.Pattern_GO[j, i].transform.parent = part.transform;
                            }
                        }
                    }

                    if (part.transform.childCount == 0)
                        Destroy(part.gameObject);

                    //Debug.Log(allData);
                }
                // 4칸이며 3칸이 아니면
                else if (isCheckPixel_4 && isCheckPixel_3 == false)
                {
                    string allData = "isCheckPixel_3 : " + isCheckPixel_3 + "idx : " + idx + " \n";
                    GameObject part = new GameObject();
                    part.transform.parent = GAM.transform;

                    part.name = idx.ToString();

                    for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
                    {
                        for (int j = 0; j < MapManager.S.mapPixel_X; j++)
                        {
                            if (curr[j, i] == idx)
                            {
                                allData += "[" + j + "," + i + "] \n";
                                MapManager.S.Pattern_GO[j, i].transform.parent = part.transform;
                            }
                        }
                    }

                    if (part.transform.childCount == 0)
                        Destroy(part.gameObject);

                    // Debug.Log(allData);
                }
                // 나머지 연산
                else
                {
                    if (GAM != null)
                    {
                        string allData = "Else : idx : " + idx + " \n";
                        if (GAM.gameObject.transform.Find(idx.ToString()) == null)
                        {
                            GameObject part = new GameObject();
                            part.transform.parent = GAM.transform;

                            part.name = idx.ToString();

                            for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
                            {
                                for (int j = 0; j < MapManager.S.mapPixel_X; j++)
                                {
                                    if (_ColorMap.map[j, i] == idx)
                                    {
                                        //allData += "[" + j + "," + i + "] \n";
                                        MapManager.S.Pattern_GO[j, i].transform.parent = part.transform;
                                    }
                                }
                            }

                            if (part.transform.childCount == 0)
                                Destroy(part.gameObject);
                        }
                        else
                        {
                            GameObject target = GAM.gameObject.transform.Find(idx.ToString()).gameObject;

                            for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
                            {
                                for (int j = 0; j < MapManager.S.mapPixel_X; j++)
                                {
                                    if (_ColorMap.map[j, i] == idx)
                                    {
                                        //allData += "[" + j + "," + i + "] \n";
                                        MapManager.S.Pattern_GO[j, i].transform.parent = target.transform;
                                    }
                                }
                            }

                            if (target.transform.childCount == 0)
                                Destroy(target.gameObject);
                        }
                    }
                }

                if (MatchToSetNum[colorIndex] - 1 == idx % 1000)
                {
                    int setIdx = idx - MatchToSetNum[colorIndex];
                    setIdx += 1000;
                    idx = setIdx;
                }
            }

            if (isNo == false)
            {
                int num = 0;
                // 각 블록에 연결될 블록들라인들의 집합 오브젝트
                GameObject item = new GameObject();
                item.transform.parent = LINE_MANAGER.transform;
                item.name = "Item";

                // 실제 블록 위치에 저장된 이름을 확인하여 블록 정보 저장
                for (int i = 0; i < GAM.transform.childCount; i++)
                {
                    Color backColor = Color.white;
                    Color textColor = Color.white;
                    int selectNum = -1;

                    int val = int.Parse(GAM.transform.GetChild(i).gameObject.name);
                    // AM1
                    if (val >= 1000 && val < 2000)
                    {
                        selectNum = 0;
                    }
                    // D1
                    else if (val >= 2000 && val < 3000)
                    {
                        selectNum = 1;
                    }
                    // TR1
                    else if (val >= 3000 && val < 4000)
                    {
                        selectNum = 2;
                    }
                    // TR2
                    else if (val >= 4000 && val < 5000)
                    {
                        selectNum = 3;
                    }
                    // TE1
                    else if (val >= 5000 && val < 6000)
                    {
                        selectNum = 4;
                    }
                    // TE2
                    else if (val >= 6000 && val < 7000)
                    {
                        selectNum = 5;
                    }
                    // TE3
                    else if (val >= 7000 && val < 8000)
                    {
                        selectNum = 6;
                    }
                    // TE4
                    else if (val >= 8000 && val < 9000)
                    {
                        selectNum = 7;
                    }
                    // TE5
                    else if (val >= 9000 && val < 10000)
                    {
                        selectNum = 8;
                    }
                    // TE6
                    else if (val >= 10000 && val < 11000)
                    {
                        selectNum = 9;
                    }
                    // TE7
                    else if (val >= 11000 && val < 12000)
                    {
                        selectNum = 10;
                    }
                    // PENTOMINO
                    else if (Option_PentoMode.isOn)
                    {
                        // PT1
                        if (val >= 12000 && val < 13000)
                        {
                            selectNum = 11;
                        }
                        // PT2
                        else if (val >= 13000 && val < 14000)
                        {
                            selectNum = 12;
                        }
                        // PT3
                        else if (val >= 14000 && val < 15000)
                        {
                            selectNum = 13;
                        }
                        // PT4
                        else if (val >= 15000 && val < 16000)
                        {
                            selectNum = 14;
                        }
                        // PT5
                        else if (val >= 16000 && val < 17000)
                        {
                            selectNum = 15;
                        }
                    }

                    // 3D 블록 색상 및 텍스트 컬러 변경
                    int backNum = val % 1000;
                    backColor = COLOR_INDEXS[selectNum].background_Color;
                    textColor = COLOR_INDEXS[selectNum].text_Color;

                    for (int j = 0; j < GAM.transform.GetChild(i).transform.childCount; j++)
                    {
                        GAM.transform.GetChild(i).transform.GetChild(j).gameObject.transform.GetChild(1).gameObject.SetActive(true);
                        GAM.transform.GetChild(i).transform.GetChild(j).gameObject.transform.GetChild(1).gameObject.GetComponent<Renderer>().material.color = backColor;
                        GAM.transform.GetChild(i).transform.GetChild(j).gameObject.transform.GetChild(1).gameObject.transform.GetChild(0).gameObject.GetComponent<TextMesh>().color = textColor;
                        GAM.transform.GetChild(i).transform.GetChild(j).gameObject.transform.GetChild(1).gameObject.transform.GetChild(0).gameObject.GetComponent<TextMesh>().text = selectNum.ToString();
                        //GAM.transform.GetChild(i).transform.GetChild(j).gameObject.transform.GetChild(1).gameObject.transform.GetChild(0).gameObject.GetComponent<TextMesh>().text = selectNum + "_" + backNum;
                    }
                    num++;

                    //Draw Line..================================================================================
                    List<Vector2> tempList = new List<Vector2>();
                    List<Vector2> itemList = new List<Vector2>();

                    // 3D 블록들의 위치 값 저장
                    for (int j = 0; j < GAM.transform.GetChild(i).gameObject.transform.childCount; j++)
                    {
                        if (int.Parse(GAM.transform.GetChild(i).gameObject.name) != 1000)
                        {
                            tempList.Add(new Vector2(GAM.transform.GetChild(i).gameObject.transform.GetChild(j).transform.position.x,
                                                                             GAM.transform.GetChild(i).gameObject.transform.GetChild(j).transform.position.y));
                        }
                    }

                    int loopCount = tempList.Count;
                    // 블록이 존재하면 실행
                    if (loopCount != 0) 
                    {
                        for (int lc = 0; lc < loopCount; lc++)
                        {
                            int fIndex = 0;
                            Vector2 fVec = tempList[0];

                            for (int j = tempList.Count - 1; j >= 0; j--)
                            {
                                if (fVec.y == tempList[j].y)
                                {
                                    if (fVec.x >= tempList[j].x)
                                    {
                                        fIndex = j;
                                        fVec = tempList[j];
                                    }
                                }

                                else if (fVec.y <= tempList[j].y)
                                {
                                    fIndex = j;
                                    fVec = tempList[j];
                                }
                            }

                            itemList.Add(fVec);
                            tempList.RemoveAt(fIndex);
                        }

                        //for (int cc = 0; cc < itemList.Count; cc++)
                        //{
                        //    Debug.Log(cc + " => " + itemList[cc]);
                        //}

                        // 라인랜더러를 넣는 부모 오브젝트
                        GameObject go = new GameObject();
                        go.transform.parent = item.transform;
                        go.name = "LineRendererParent";

                        /* 2020.08.14 수정
                         * 두 블록 사이를 2개의 라인랜더러로 연결하는 방식에서 한개만으로 연결되도록 수정
                         */
                        // 라인랜더러 오브젝트 생성 및 라인 위치, 색상 설정
                        for (int k = 0; k < itemList.Count; k++)
                        {
                            for (int j = k; j < itemList.Count; j++)
                            {
                                if (k == j) continue;

                                if (((itemList[k].x == itemList[j].x) && (Mathf.Abs(itemList[k].y - itemList[j].y) == 1)) ||
                                   ((itemList[k].y == itemList[j].y) && (Mathf.Abs(itemList[k].x - itemList[j].x) == 1)))
                                {
                                    // 라인랜더러 오브젝트
                                    GameObject childLineRenderer = new GameObject();
                                    // 부모 선언
                                    childLineRenderer.transform.parent = go.transform;
                                    // 이름 변경
                                    childLineRenderer.name = "Line " + (k);

                                    childLineRenderer.gameObject.AddComponent<LineRenderer>();
                                    LineRenderer myLine = childLineRenderer.gameObject.GetComponent<LineRenderer>();

                                    // 라인 설정
                                    myLine.positionCount = 2;
                                    myLine.startWidth = 0.3f;
                                    myLine.endWidth = 0.3f;

                                    Vector3 newPos = itemList[j];
                                    newPos.z = -5.0f;

                                    myLine.SetPosition(0, newPos);

                                    newPos = itemList[k];
                                    newPos.z = -5.0f;

                                    myLine.SetPosition(1, newPos);
                                }
                            }
                        }

                        // 라인 오브젝트 최상위 부모 오브젝트 활성화
                        LINE_MANAGER.gameObject.SetActive(true);

                        if (isEqueal == false)
                        {

                            if (BTN_OK.gameObject.activeSelf)
                            {
                                //Debug.Log(isCheckPixel_3 + "," + isCheckPixel_4);
                                //스크린샷 캡처 및 저장(ID_COLOR_INDEX.png)
                                fullName = "";
                                fullName += GracesGames.SimpleFileBrowser.Scripts.TextFileFinder.S.FileName.text + "_";

                                switch (colorIndex)
                                {
                                    case 1:
                                        fullName += "YE";
                                        break;

                                    case 2:
                                        fullName += "OR";
                                        break;

                                    case 3:
                                        fullName += "GN";
                                        break;

                                    case 4:
                                        fullName += "BU";
                                        break;

                                    case 5:
                                        fullName += "RD";
                                        break;

                                    case 6:
                                        fullName += "BR";
                                        break;

                                    case 7:
                                        fullName += "WH";
                                        break;

                                    case 8:
                                        fullName += "BK";
                                        break;

                                    case 9:
                                        fullName += "GY";
                                        break;

                                    case 10:
                                        fullName += "NV";
                                        break;

                                    case 11:
                                        fullName += "SB";
                                        break;

                                    case 12:
                                        fullName += "HP";
                                        break;

                                    case 13:
                                        fullName += "DB";
                                        break;
                                }

                                fullName += ConstrastColorMapNumOf;
                                saveFileName = fullName + ".png";
                                //Debug.Log("Time : " + eTime);

                                //ScreenShot 폴더로이동
                                string saveByPath = Application.dataPath;

                                //myResultText.text = saveByPath;
                                //return;

#if UNITY_EDITOR
                                saveByPath = saveByPath.Substring(0, saveByPath.IndexOf("Assets"));
                                saveByPath += "ScreenShot/";
#elif UNITY_STANDALONE_WIN
                            saveByPath += "/ScreenShot/";
#endif
                                //myResultText.text = System.IO.Directory.GetCurrentDirectory() + "/PIXELMAKER_Data";
                                //return;

                                saveByPath += fullName + ".png";
                                //Debug.Log(saveByPath);
                                ScreenCapture.CaptureScreenshot(saveByPath);
                            }
                        }
                    }
                }

                isOKCheck = true;
            }
            else
            {
                isOKCheck = false;
            }

            //XML 추출작업 수행 ***
            //Debug.Log("XML 추출작업 수행");
            List<string> XML_DATA_LIST = new List<string>();
            string data = "";


            string map = "";
            string blockName = "AM1";
            int rot = 0;
            for (int i = 0; i < sizeY; i++)
            {
                map += "\n";
                for (int j = 0; j < sizeX; j++)
                {
                    if (_ColorMap.map[j, i] > 0)
                        map += "X";
                    else
                        map += "O";

                    // 블록 정보가 저장되지 않은 블록은 넘어감
                    // * 1000~16000까지 1000단위마다 D1~PT5까지 블록을 의미
                    // * 0~13까지 블록 색상 단위
                    // * 99는 색상이 없는 위치
                    if (_ColorMap.map[j, i] < 1000) continue;

                    int colorMapIndex = _ColorMap.map[j, i];
                    data = "";
                    rot = 0;

                    int X = j;
                    int Y = i;

                    // XML 정보 설정
                    // AM1
                    if (colorMapIndex >= 1000 && colorMapIndex < 2000)
                    {
                        blockName = "AM1";
                        rot = 0;

                        data += "<id>" + blockName + "</id>\n";
                        data += "<color>" + color + "</color>\n";
                        data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                        XML_DATA_LIST.Add(data);
                        _ColorMap.map[X, Y] = -1;
                    }
                    // D1
                    else if (colorMapIndex >= 2000 && colorMapIndex < 3000)
                    {
                        blockName = "D1";

                        data += "<id>" + blockName + "</id>\n";
                        data += "<color>" + color + "</color>\n";

                        //D1 - R0
                        //ㅍ
                        //ㅁ
                        if (
                           (0 <= Y - 1 && _ColorMap.map[X, Y - 1] == colorMapIndex))
                        {
                            rot = 0;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X, Y - 1] = -1;
                        }

                        //D1 - R1
                        //ㅍㅁ
                        else if (
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y] == colorMapIndex))
                        {
                            rot = 1;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X + 1, Y] = -1;
                        }

                        //D1 - R2
                        //ㅁ
                        //ㅍ
                        else if (
                           (sizeY > Y + 1 && _ColorMap.map[X, Y + 1] == colorMapIndex))
                        {
                            rot = 2;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X, Y + 1] = -1;
                        }

                        //D1 - R3
                        //ㅁㅍ
                        else if (
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y] == colorMapIndex))
                        {
                            rot = 3;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X - 1, Y] = -1;
                        }
                    }
                    // TR1
                    else if (colorMapIndex >= 3000 && colorMapIndex < 4000)
                    {
                        blockName = "TR1";
                        data += "<id>" + blockName + "</id>\n";
                        data += "<color>" + color + "</color>\n";

                        //TR1 - R0
                        //ㅍ
                        //ㅁ
                        //ㅁ
                        if (
                                (0 <= Y - 1 && _ColorMap.map[X, Y - 1] == colorMapIndex) &&
                                (0 <= Y - 2 && _ColorMap.map[X, Y - 2] == colorMapIndex))
                        {
                            rot = 0;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X, Y - 1] = -1;
                            _ColorMap.map[X, Y - 2] = -1;
                        }

                        //TR1 - R1
                        //ㅍㅁㅁ
                        else if (
                               (sizeX > X + 1 && _ColorMap.map[X + 1, Y] == colorMapIndex) &&
                               (sizeX > X + 2 && _ColorMap.map[X + 2, Y] == colorMapIndex))
                        {
                            rot = 1;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X + 1, Y] = -1;
                            _ColorMap.map[X + 2, Y] = -1;
                        }

                        //TR1 - R2
                        //ㅁ
                        //ㅁ
                        //ㅍ
                        else if (
                                (sizeY > Y + 1 && _ColorMap.map[X, Y + 1] == colorMapIndex) &&
                                (sizeY > Y + 2 && _ColorMap.map[X, Y + 2] == colorMapIndex))
                        {
                            rot = 2;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X, Y + 1] = -1;
                            _ColorMap.map[X, Y + 2] = -1;
                        }

                        //TR1 - R3
                        //ㅁㅁㅍ
                        else if (
                                (0 <= X - 1 && _ColorMap.map[X - 1, Y] == colorMapIndex) &&
                                (0 <= X - 2 && _ColorMap.map[X - 2, Y] == colorMapIndex))
                        {
                            rot = 3;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X - 1, Y] = -1;
                            _ColorMap.map[X - 2, Y] = -1;
                        }
                    }
                    // TR2
                    else if (colorMapIndex >= 4000 && colorMapIndex < 5000)
                    {
                        blockName = "TR2";
                        data += "<id>" + blockName + "</id>\n";
                        data += "<color>" + color + "</color>\n";

                        //TR2 - 0
                        //ㅁㅍ
                        //ㅁ
                        if (
                                (0 <= X - 1 && _ColorMap.map[X - 1, Y] == colorMapIndex) &&
                                (0 <= Y - 1 && _ColorMap.map[X - 1, Y - 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X - 1, Y));
                            CheckList.Add(new Vector2(X - 1, Y - 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 0;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X - 1, Y] = -1;
                                _ColorMap.map[X - 1, Y - 1] = -1;
                            }
                        }

                        //TR2 - 1
                        //ㅍ
                        //ㅁㅁ
                        if (
                                (0 <= Y - 1 && _ColorMap.map[X, Y - 1] == colorMapIndex) &&
                                (sizeX > X + 1 && _ColorMap.map[X + 1, Y - 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y - 1));
                            CheckList.Add(new Vector2(X + 1, Y - 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 1;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y - 1] = -1;
                                _ColorMap.map[X + 1, Y - 1] = -1;
                            }
                        }

                        //TR2 - 2
                        //  ㅁ
                        //ㅍㅁ
                        if (
                                (sizeX > X + 1 && _ColorMap.map[X + 1, Y] == colorMapIndex) &&
                                (sizeY > Y + 1 && _ColorMap.map[X + 1, Y + 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X + 1, Y));
                            CheckList.Add(new Vector2(X + 1, Y + 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 2;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X + 1, Y] = -1;
                                _ColorMap.map[X + 1, Y + 1] = -1;
                            }
                        }

                        //TR2 - 3
                        //ㅁㅁ
                        //  ㅍ
                        if (
                                (sizeY > Y + 1 && _ColorMap.map[X, Y + 1] == colorMapIndex) &&
                                (0 <= X - 1 && _ColorMap.map[X - 1, Y + 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y + 1));
                            CheckList.Add(new Vector2(X - 1, Y + 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 3;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y + 1] = -1;
                                _ColorMap.map[X - 1, Y + 1] = -1;
                            }
                        }
                    }
                    // TE1
                    else if (colorMapIndex >= 5000 && colorMapIndex < 6000)
                    {
                        blockName = "TE1";
                        data += "<id>" + blockName + "</id>\n";
                        data += "<color>" + color + "</color>\n";

                        //TE1 - R0
                        //ㅁㅁㅁㅍ
                        if (
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y] == colorMapIndex) &&
                           (0 <= X - 2 && _ColorMap.map[X - 2, Y] == colorMapIndex) &&
                           (0 <= X - 3 && _ColorMap.map[X - 3, Y] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X - 1, Y));
                            CheckList.Add(new Vector2(X - 2, Y));
                            CheckList.Add(new Vector2(X - 3, Y));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 0;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X - 1, Y] = -1;
                                _ColorMap.map[X - 2, Y] = -1;
                                _ColorMap.map[X - 3, Y] = -1;
                            }
                        }

                        //TE1 - R1
                        //ㅍㅁㅁㅁ
                        if (
                            (sizeX > X + 1 && _ColorMap.map[X + 1, Y] == colorMapIndex) &&
                            (sizeX > X + 2 && _ColorMap.map[X + 2, Y] == colorMapIndex) &&
                            (sizeX > X + 3 && _ColorMap.map[X + 3, Y] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X + 1, Y));
                            CheckList.Add(new Vector2(X + 2, Y));
                            CheckList.Add(new Vector2(X + 3, Y));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 1;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X + 1, Y] = -1;
                                _ColorMap.map[X + 2, Y] = -1;
                                _ColorMap.map[X + 3, Y] = -1;
                            }
                        }

                        //TE1 - R2
                        //ㅁ
                        //ㅁ
                        //ㅁ
                        //ㅍ
                        if (
                           (sizeY > Y + 1 && _ColorMap.map[X, Y + 1] == colorMapIndex) &&
                           (sizeY > Y + 2 && _ColorMap.map[X, Y + 2] == colorMapIndex) &&
                           (sizeY > Y + 3 && _ColorMap.map[X, Y + 3] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y + 1));
                            CheckList.Add(new Vector2(X, Y + 2));
                            CheckList.Add(new Vector2(X, Y + 3));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 2;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y + 1] = -1;
                                _ColorMap.map[X, Y + 2] = -1;
                                _ColorMap.map[X, Y + 3] = -1;
                            }
                        }

                        //TE1 - R3
                        //ㅍ
                        //ㅁ
                        //ㅁ
                        //ㅁ
                        if (
                           (0 <= Y - 1 && _ColorMap.map[X, Y - 1] == colorMapIndex) &&
                           (0 <= Y - 2 && _ColorMap.map[X, Y - 2] == colorMapIndex) &&
                           (0 <= Y - 3 && _ColorMap.map[X, Y - 3] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y - 1));
                            CheckList.Add(new Vector2(X, Y - 2));
                            CheckList.Add(new Vector2(X, Y - 3));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 3;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y - 1] = -1;
                                _ColorMap.map[X, Y - 2] = -1;
                                _ColorMap.map[X, Y - 3] = -1;
                            }
                        }
                    }
                    // TE2
                    else if (colorMapIndex >= 6000 && colorMapIndex < 7000)
                    {
                        blockName = "TE2";
                        data += "<id>" + blockName + "</id>\n";
                        data += "<color>" + color + "</color>\n";

                        //TE2 - R0
                        //ㅍㅁㅁ
                        //    ㅁ
                        if (
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y] == colorMapIndex) &&
                           (sizeX > X + 2 && _ColorMap.map[X + 2, Y] == colorMapIndex) &&
                           (0 <= Y - 1 && _ColorMap.map[X + 2, Y - 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X + 1, Y));
                            CheckList.Add(new Vector2(X + 2, Y));
                            CheckList.Add(new Vector2(X + 2, Y - 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 0;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X + 1, Y] = -1;
                                _ColorMap.map[X + 2, Y] = -1;
                                _ColorMap.map[X + 2, Y - 1] = -1;
                            }
                        }

                        //TE2 - R1
                        //ㅁㅁ
                        //ㅁ  
                        //ㅍ
                        if (
                           (sizeY > Y + 1 && _ColorMap.map[X, Y + 1] == colorMapIndex) &&
                           (sizeY > Y + 2 && _ColorMap.map[X, Y + 2] == colorMapIndex) &&
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y + 2] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y + 1));
                            CheckList.Add(new Vector2(X, Y + 2));
                            CheckList.Add(new Vector2(X + 1, Y + 2));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 1;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y + 1] = -1;
                                _ColorMap.map[X, Y + 2] = -1;
                                _ColorMap.map[X + 1, Y + 2] = -1;
                            }
                        }

                        //TE2 - R2
                        //ㅁ    
                        //ㅁㅁㅍ
                        if (
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y] == colorMapIndex) &&
                           (0 <= X - 2 && _ColorMap.map[X - 2, Y] == colorMapIndex) &&
                           (sizeY > Y + 1 && _ColorMap.map[X - 2, Y + 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X - 1, Y));
                            CheckList.Add(new Vector2(X - 2, Y));
                            CheckList.Add(new Vector2(X - 2, Y + 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 2;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X - 1, Y] = -1;
                                _ColorMap.map[X - 2, Y] = -1;
                                _ColorMap.map[X - 2, Y + 1] = -1;
                            }
                        }

                        //TE2 - R3
                        //  ㅍ
                        //  ㅁ  
                        //ㅁㅁ
                        if (
                           (0 <= Y - 1 && _ColorMap.map[X, Y - 1] == colorMapIndex) &&
                           (0 <= Y - 2 && _ColorMap.map[X, Y - 2] == colorMapIndex) &&
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y - 2] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y - 1));
                            CheckList.Add(new Vector2(X, Y - 2));
                            CheckList.Add(new Vector2(X - 1, Y - 2));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 3;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y - 1] = -1;
                                _ColorMap.map[X, Y - 2] = -1;
                                _ColorMap.map[X - 1, Y - 2] = -1;
                            }
                        }
                    }
                    // TE3
                    else if (colorMapIndex >= 7000 && colorMapIndex < 8000)
                    {
                        blockName = "TE3";
                        data += "<id>" + blockName + "</id>\n";
                        data += "<color>" + color + "</color>\n";

                        //TE3 - R0
                        if (
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y] == colorMapIndex) &&
                           (0 <= X - 2 && _ColorMap.map[X - 2, Y] == colorMapIndex) &&
                           (0 <= Y - 1 && _ColorMap.map[X - 2, Y - 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X - 1, Y));
                            CheckList.Add(new Vector2(X - 2, Y));
                            CheckList.Add(new Vector2(X - 2, Y - 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 0;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X - 1, Y] = -1;
                                _ColorMap.map[X - 2, Y] = -1;
                                _ColorMap.map[X - 2, Y - 1] = -1;
                            }
                        }

                        //TE3 - R1
                        if (
                           (0 <= Y - 1 && _ColorMap.map[X, Y - 1] == colorMapIndex) &&
                           (0 <= Y - 2 && _ColorMap.map[X, Y - 2] == colorMapIndex) &&
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y - 2] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y - 1));
                            CheckList.Add(new Vector2(X, Y - 2));
                            CheckList.Add(new Vector2(X + 1, Y - 2));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 1;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y - 1] = -1;
                                _ColorMap.map[X, Y - 2] = -1;
                                _ColorMap.map[X + 1, Y - 2] = -1;
                            }
                        }

                        //TE3 - R2
                        if (
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y] == colorMapIndex) &&
                           (sizeX > X + 2 && _ColorMap.map[X + 2, Y] == colorMapIndex) &&
                           (sizeY > Y + 1 && _ColorMap.map[X + 2, Y + 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X + 1, Y));
                            CheckList.Add(new Vector2(X + 2, Y));
                            CheckList.Add(new Vector2(X + 2, Y + 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 2;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X + 1, Y] = -1;
                                _ColorMap.map[X + 2, Y] = -1;
                                _ColorMap.map[X + 2, Y + 1] = -1;
                            }
                        }

                        //TE3 - R3
                        if (
                           (sizeY > Y + 1 && _ColorMap.map[X, Y + 1] == colorMapIndex) &&
                           (sizeY > Y + 2 && _ColorMap.map[X, Y + 2] == colorMapIndex) &&
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y + 2] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y + 1));
                            CheckList.Add(new Vector2(X, Y + 2));
                            CheckList.Add(new Vector2(X - 1, Y + 2));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 3;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y + 1] = -1;
                                _ColorMap.map[X, Y + 2] = -1;
                                _ColorMap.map[X - 1, Y + 2] = -1;
                            }
                        }
                    }
                    // TE4
                    else if (colorMapIndex >= 8000 && colorMapIndex < 9000)
                    {
                        blockName = "TE4";
                        data += "<id>" + blockName + "</id>\n";
                        data += "<color>" + color + "</color>\n";

                        //TE4 - R0
                        //ㅁㅍ
                        //ㅁㅁ
                        if (
                            0 <= X - 1 &&
                            0 <= Y - 1 &&

                            (_ColorMap.map[X - 1, Y] == colorMapIndex) &&
                            (_ColorMap.map[X - 1, Y - 1] == colorMapIndex) &&
                            (_ColorMap.map[X, Y - 1] == colorMapIndex)
                           )
                        {
                            rot = 0;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X - 1, Y] = -1;
                            _ColorMap.map[X - 1, Y - 1] = -1;
                            _ColorMap.map[X, Y - 1] = -1;
                        }

                        //TE4 - R1
                        //ㅍㅁ
                        //ㅁㅁ
                        if (
                            0 <= Y - 1 &&
                            sizeX > X + 1 &&

                           (_ColorMap.map[X + 1, Y] == colorMapIndex) &&
                           (_ColorMap.map[X + 1, Y - 1] == colorMapIndex) &&
                           (_ColorMap.map[X, Y - 1] == colorMapIndex)
                           )
                        {
                            rot = 1;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X + 1, Y] = -1;
                            _ColorMap.map[X + 1, Y - 1] = -1;
                            _ColorMap.map[X, Y - 1] = -1;
                        }

                        //TE4 - R2
                        //ㅁㅁ
                        //ㅍㅁ
                        if (
                            sizeX > X + 1 &&
                            sizeY > Y + 1 &&

                           (_ColorMap.map[X + 1, Y] == colorMapIndex) &&
                           (_ColorMap.map[X + 1, Y + 1] == colorMapIndex) &&
                           (_ColorMap.map[X, Y + 1] == colorMapIndex)
                           )
                        {
                            rot = 2;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X + 1, Y] = -1;
                            _ColorMap.map[X + 1, Y + 1] = -1;
                            _ColorMap.map[X, Y + 1] = -1;
                        }

                        //TE4 - R3
                        //ㅁㅁ
                        //ㅁㅍ
                        if (
                            0 <= X - 1 && 
                            sizeY > Y + 1 &&

                            (_ColorMap.map[X, Y + 1] == colorMapIndex) &&
                            (_ColorMap.map[X - 1, Y + 1] == colorMapIndex) &&
                            (_ColorMap.map[X - 1, Y] == colorMapIndex)
                            )
                        {
                            rot = 3;
                            data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                            XML_DATA_LIST.Add(data);

                            _ColorMap.map[X, Y] = -1;
                            _ColorMap.map[X, Y + 1] = -1;
                            _ColorMap.map[X - 1, Y + 1] = -1;
                            _ColorMap.map[X - 1, Y] = -1;
                        }

                    }
                    // TE5
                    else if (colorMapIndex >= 9000 && colorMapIndex < 10000)
                    {
                        blockName = "TE5";
                        data += "<id>" + blockName + "</id>\n";
                        data += "<color>" + color + "</color>\n";

                        //TE5 - R0
                        //ㅍ
                        //ㅁㅁ
                        //  ㅁ
                        if (
                           (0 <= Y - 1 && _ColorMap.map[X, Y - 1] == colorMapIndex) &&
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y - 1] == colorMapIndex) &&
                           (0 <= Y - 2 && _ColorMap.map[X + 1, Y - 2] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y - 1));
                            CheckList.Add(new Vector2(X + 1, Y - 1));
                            CheckList.Add(new Vector2(X + 1, Y - 2));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 0;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y - 1] = -1;
                                _ColorMap.map[X + 1, Y - 1] = -1;
                                _ColorMap.map[X + 1, Y - 2] = -1;
                            }
                        }

                        //TE5 - R1
                        //  ㅁㅁ
                        //ㅍㅁ
                        if (
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y] == colorMapIndex) &&
                           (sizeY > Y + 1 && _ColorMap.map[X + 1, Y + 1] == colorMapIndex) &&
                           (sizeX > X + 2 && _ColorMap.map[X + 2, Y + 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X + 1, Y));
                            CheckList.Add(new Vector2(X + 1, Y + 1));
                            CheckList.Add(new Vector2(X + 2, Y + 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 1;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X + 1, Y] = -1;
                                _ColorMap.map[X + 1, Y + 1] = -1;
                                _ColorMap.map[X + 2, Y + 1] = -1;
                            }
                        }

                        //TE5 - R2
                        //ㅁ
                        //ㅁㅁ
                        //  ㅍ
                        if (
                           (sizeY > Y + 1 && _ColorMap.map[X, Y + 1] == colorMapIndex) &&
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y + 1] == colorMapIndex) &&
                           (sizeY > Y + 2 && _ColorMap.map[X - 1, Y + 2] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y + 1));
                            CheckList.Add(new Vector2(X - 1, Y + 1));
                            CheckList.Add(new Vector2(X - 1, Y + 2));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 2;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y + 1] = -1;
                                _ColorMap.map[X - 1, Y + 1] = -1;
                                _ColorMap.map[X - 1, Y + 2] = -1;
                            }
                        }

                        //TE5 - R3
                        //  ㅁㅍ
                        //ㅁㅁ
                        if (
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y] == colorMapIndex) &&
                           (0 <= Y - 1 && _ColorMap.map[X - 1, Y - 1] == colorMapIndex) &&
                           (0 <= X - 2 && _ColorMap.map[X - 2, Y - 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X - 1, Y));
                            CheckList.Add(new Vector2(X - 1, Y - 1));
                            CheckList.Add(new Vector2(X - 2, Y - 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 3;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X - 1, Y] = -1;
                                _ColorMap.map[X - 1, Y - 1] = -1;
                                _ColorMap.map[X - 2, Y - 1] = -1;
                            }
                        }
                    }
                    // TE6
                    else if (colorMapIndex >= 10000 && colorMapIndex < 11000)
                    {
                        blockName = "TE6";
                        data += "<id>" + blockName + "</id>\n";
                        data += "<color>" + color + "</color>\n";

                        //TE6 - R0
                        //ㅁ
                        //ㅍㅁ
                        //ㅁ
                        if (
                           (sizeY > Y + 1 && _ColorMap.map[X, Y + 1] == colorMapIndex) &&
                           (0 <= Y - 1 && _ColorMap.map[X, Y - 1] == colorMapIndex) &&
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y + 1));
                            CheckList.Add(new Vector2(X, Y - 1));
                            CheckList.Add(new Vector2(X + 1, Y));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count != 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 0;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y + 1] = -1;
                                _ColorMap.map[X, Y - 1] = -1;
                                _ColorMap.map[X + 1, Y] = -1;
                            }
                        }

                        //TE6 - R1
                        //  ㅁ
                        //ㅁㅍㅁ
                        if (
                           (sizeY > Y + 1 && _ColorMap.map[X, Y + 1] == colorMapIndex) &&
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y] == colorMapIndex) &&
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y + 1));
                            CheckList.Add(new Vector2(X + 1, Y));
                            CheckList.Add(new Vector2(X - 1, Y));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count != 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 1;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y + 1] = -1;
                                _ColorMap.map[X + 1, Y] = -1;
                                _ColorMap.map[X - 1, Y] = -1;
                            }
                        }

                        //TE6 - R2
                        //  ㅁ
                        //ㅁㅍ
                        //  ㅁ
                        if (
                           (sizeY > Y + 1 && _ColorMap.map[X, Y + 1] == colorMapIndex) &&
                           (0 <= Y - 1 && _ColorMap.map[X, Y - 1] == colorMapIndex) &&
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y + 1));
                            CheckList.Add(new Vector2(X, Y - 1));
                            CheckList.Add(new Vector2(X - 1, Y));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count != 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 2;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y + 1] = -1;
                                _ColorMap.map[X, Y - 1] = -1;
                                _ColorMap.map[X - 1, Y] = -1;
                            }
                        }

                        //TE6 - R3
                        //ㅁㅍㅁ
                        //  ㅁ
                        if (
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y] == colorMapIndex) &&
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y] == colorMapIndex) &&
                           (0 <= Y - 1 && _ColorMap.map[X, Y - 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X - 1, Y));
                            CheckList.Add(new Vector2(X + 1, Y));
                            CheckList.Add(new Vector2(X, Y - 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                        ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count != 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 3;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X - 1, Y] = -1;
                                _ColorMap.map[X + 1, Y] = -1;
                                _ColorMap.map[X, Y - 1] = -1;
                            }
                        }
                    }
                    // TE7
                    else if (colorMapIndex >= 11000 && colorMapIndex < 12000)
                    {
                        blockName = "TE7";
                        data += "<id>" + blockName + "</id>\n";
                        data += "<color>" + color + "</color>\n";

                        //TE7 - R0
                        //  ㅍ
                        //ㅁㅁ
                        //ㅁ
                        if (
                           (0 <= Y - 1 && _ColorMap.map[X, Y - 1] == colorMapIndex) &&
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y - 1] == colorMapIndex) &&
                           (0 <= Y - 2 && _ColorMap.map[X - 1, Y - 2] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y - 1));
                            CheckList.Add(new Vector2(X - 1, Y - 1));
                            CheckList.Add(new Vector2(X - 1, Y - 2));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 0;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y - 1] = -1;
                                _ColorMap.map[X - 1, Y - 1] = -1;
                                _ColorMap.map[X - 1, Y - 2] = -1;
                            }
                        }

                        //TE7 - R1
                        //ㅍㅁ
                        //  ㅁㅁ
                        if (
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y] == colorMapIndex) &&
                           (0 <= Y - 1 && _ColorMap.map[X + 1, Y - 1] == colorMapIndex) &&
                           (sizeX > X + 2 && _ColorMap.map[X + 2, Y - 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X + 1, Y));
                            CheckList.Add(new Vector2(X + 1, Y - 1));
                            CheckList.Add(new Vector2(X + 2, Y - 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 1;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X + 1, Y] = -1;
                                _ColorMap.map[X + 1, Y - 1] = -1;
                                _ColorMap.map[X + 2, Y - 1] = -1;
                            }
                        }

                        //TE7 - R2
                        //  ㅁ
                        //ㅁㅁ
                        //ㅍ
                        if (
                           (sizeY > Y + 1 && _ColorMap.map[X, Y + 1] == colorMapIndex) &&
                           (sizeX > X + 1 && _ColorMap.map[X + 1, Y + 1] == colorMapIndex) &&
                           (sizeY > Y + 2 && _ColorMap.map[X + 1, Y + 2] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X, Y + 1));
                            CheckList.Add(new Vector2(X + 1, Y + 1));
                            CheckList.Add(new Vector2(X + 1, Y + 2));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 2;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X, Y + 1] = -1;
                                _ColorMap.map[X + 1, Y + 1] = -1;
                                _ColorMap.map[X + 1, Y + 2] = -1;
                            }
                        }

                        //TE7 - R3
                        //ㅁㅁ
                        //  ㅁㅍ
                        if (
                           (0 <= X - 1 && _ColorMap.map[X - 1, Y] == colorMapIndex) &&
                           (sizeY > Y + 1 && _ColorMap.map[X - 1, Y + 1] == colorMapIndex) &&
                           (0 <= X - 2 && _ColorMap.map[X - 2, Y + 1] == colorMapIndex))
                        {
                            bool isAvaliable = false;
                            List<Vector2> CheckList = new List<Vector2>();
                            CheckList.Add(new Vector2(X - 1, Y));
                            CheckList.Add(new Vector2(X - 1, Y + 1));
                            CheckList.Add(new Vector2(X - 2, Y + 1));

                            for (int n = 0; n < CheckList.Count; n++)
                            {
                                int count = 0;
                                for (int c = 0; c < CheckList.Count; c++)
                                {
                                    if (n == c) continue;

                                    if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                    {
                                        count++;
                                    }
                                }

                                if (count == 0)
                                {
                                    isAvaliable = true;
                                    break;
                                }
                            }

                            if (isAvaliable == false)
                            {
                                rot = 3;
                                data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                XML_DATA_LIST.Add(data);

                                _ColorMap.map[X, Y] = -1;
                                _ColorMap.map[X - 1, Y] = -1;
                                _ColorMap.map[X - 1, Y + 1] = -1;
                                _ColorMap.map[X - 2, Y + 1] = -1;
                            }
                        }
                    }
                    // PENTO
                    else if (Option_PentoMode.isOn)
                    {
                        // PT1
                        if (colorMapIndex >= 12000 && colorMapIndex < 13000)
                        {
                            blockName = "PT1";
                            data += "<id>" + blockName + "</id>\n";
                            data += "<color>" + color + "</color>\n";

                            //PT1 - R0
                            //①②③
                            //  ④
                            //  ㅍ
                            if (
                                0 <= X - 1 &&
                                sizeX > X + 1 &&
                                sizeY > Y + 2 &&

                               (_ColorMap.map[X - 1, Y + 2] == colorMapIndex) &&    // 왼쪽 위   1
                               (_ColorMap.map[X + 1, Y + 2] == colorMapIndex) &&    // 오른쪽 위 2
                               (_ColorMap.map[X, Y + 1] == colorMapIndex) &&        // 중단 위   3
                               (_ColorMap.map[X, Y + 2] == colorMapIndex)           // 중단 위   4
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X - 1, Y + 2));
                                CheckList.Add(new Vector2(X + 1, Y + 2));
                                CheckList.Add(new Vector2(X, Y + 1));
                                CheckList.Add(new Vector2(X, Y + 2));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 0;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X - 1, Y + 2] = -1;
                                    _ColorMap.map[X + 1, Y + 2] = -1;
                                    _ColorMap.map[X, Y + 1] = -1;
                                    _ColorMap.map[X, Y + 2] = -1;
                                }
                            }

                            //TE7 - R1
                            //③
                            //②①ㅍ
                            //④
                            if (
                                0 <= X - 2 &&
                                0 <= Y - 1 &&
                                sizeY > Y + 1 &&

                               (_ColorMap.map[X - 1, Y] == colorMapIndex) &&        // 왼쪽 1
                               (_ColorMap.map[X - 2, Y] == colorMapIndex) &&        // 왼쪽 2 
                               (_ColorMap.map[X - 2, Y + 1] == colorMapIndex) &&    // 위   3
                               (_ColorMap.map[X - 2, Y - 1] == colorMapIndex)       // 아래 4
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X - 1, Y));
                                CheckList.Add(new Vector2(X - 2, Y));
                                CheckList.Add(new Vector2(X - 2, Y + 1));
                                CheckList.Add(new Vector2(X - 2, Y - 1));

                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || 
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                if (isAvaliable == false)
                                {
                                    rot = 1;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X - 1, Y] = -1;
                                    _ColorMap.map[X - 2, Y] = -1;
                                    _ColorMap.map[X - 2, Y + 1] = -1;
                                    _ColorMap.map[X - 2, Y - 1] = -1;
                                }
                            }

                            //TE7 - R2
                            //  ㅍ
                            //  ③
                            //①④②
                            if (
                                0 <= X - 1 &&
                                0 <= Y - 2 &&
                                sizeX > X + 1 &&

                               (_ColorMap.map[X - 1, Y - 2] == colorMapIndex) &&      // 왼쪽 아래    1
                               (_ColorMap.map[X + 1, Y - 2] == colorMapIndex) &&   // 오른쪽 아래  2
                               (_ColorMap.map[X, Y - 1] == colorMapIndex) &&          // 중단 아래    3
                               (_ColorMap.map[X, Y - 2] == colorMapIndex)             // 중단 아래    4
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X - 1, Y - 2));
                                CheckList.Add(new Vector2(X + 1, Y - 2));
                                CheckList.Add(new Vector2(X, Y - 1));
                                CheckList.Add(new Vector2(X, Y - 2));

                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                if (isAvaliable == false)
                                {
                                    rot = 2;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X - 1, Y - 2] = -1;
                                    _ColorMap.map[X + 1, Y - 2] = -1;
                                    _ColorMap.map[X, Y - 1] = -1;
                                    _ColorMap.map[X, Y - 2] = -1;
                                }
                            }

                            //TE7 - R3
                            //    ③
                            //ㅍ①②
                            //    ④
                            if (
                                0 <= Y - 1 &&
                                sizeX > X + 2 &&
                                sizeY > Y + 1 &&

                               (_ColorMap.map[X + 1, Y] == colorMapIndex) &&        // 오른쪽      1
                               (_ColorMap.map[X + 2, Y] == colorMapIndex) &&        // 오른쪽      2
                               (_ColorMap.map[X + 2, Y + 1] == colorMapIndex) &&    // 오른쪽 위   3
                               (_ColorMap.map[X + 2, Y - 1] == colorMapIndex)       // 오른쪽 아래 4
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X + 1, Y));
                                CheckList.Add(new Vector2(X + 2, Y));
                                CheckList.Add(new Vector2(X + 2, Y + 1));
                                CheckList.Add(new Vector2(X + 2, Y - 1));

                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        if (((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) || ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                if (isAvaliable == false)
                                {
                                    rot = 3;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X + 1, Y] = -1;
                                    _ColorMap.map[X + 2, Y] = -1;
                                    _ColorMap.map[X + 2, Y + 1] = -1;
                                    _ColorMap.map[X + 2, Y - 1] = -1;
                                }
                            }
                        }
                        // PT2
                        else if (colorMapIndex >= 13000 && colorMapIndex < 14000)
                        {
                            blockName = "PT2";
                            data += "<id>" + blockName + "</id>\n";
                            data += "<color>" + color + "</color>\n";

                            //PT1 - R0
                            //  ③
                            //①ㅍ②
                            //  ④
                            if (
                                0 <= X - 1 &&
                                0 <= Y - 1 &&
                                sizeX > X + 1 &&
                                sizeY > Y + 1 &&

                               (_ColorMap.map[X - 1, Y] == colorMapIndex) &&
                               (_ColorMap.map[X + 1, Y] == colorMapIndex) &&
                               (_ColorMap.map[X, Y + 1] == colorMapIndex) &&
                               (_ColorMap.map[X, Y - 1] == colorMapIndex)           
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X - 1, Y));
                                CheckList.Add(new Vector2(X + 1, Y));
                                CheckList.Add(new Vector2(X, Y + 1));
                                CheckList.Add(new Vector2(X, Y - 1));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 0;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X - 1, Y] = -1;
                                    _ColorMap.map[X + 1, Y] = -1;
                                    _ColorMap.map[X, Y + 1] = -1;
                                    _ColorMap.map[X, Y - 1] = -1;
                                }
                            }
                        }
                        // PT3
                        else if (colorMapIndex >= 14000 && colorMapIndex < 15000)
                        {
                            blockName = "PT3";
                            data += "<id>" + blockName + "</id>\n";
                            data += "<color>" + color + "</color>\n";

                            //PT3 - R0
                            //ㅍ
                            //ㅁㅁ
                            //  ㅁㅁ
                            if (
                                0 <= Y - 2 &&
                                sizeX > X + 2 &&

                               (_ColorMap.map[X, Y - 1] == colorMapIndex) &&
                               (_ColorMap.map[X + 1, Y - 1] == colorMapIndex) &&
                               (_ColorMap.map[X + 1, Y - 2] == colorMapIndex) &&
                               (_ColorMap.map[X + 2, Y - 2] == colorMapIndex)
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X, Y - 1));
                                CheckList.Add(new Vector2(X + 1, Y - 1));
                                CheckList.Add(new Vector2(X + 1, Y - 2));
                                CheckList.Add(new Vector2(X + 2, Y - 2));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 0;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X, Y - 1] = -1;
                                    _ColorMap.map[X + 1, Y - 1] = -1;
                                    _ColorMap.map[X + 1, Y - 2] = -1;
                                    _ColorMap.map[X + 2, Y - 2] = -1;
                                }
                            }

                            //PT3 - R1
                            //    ④
                            //  ②③
                            //ㅍ①
                            if (
                                sizeX > X + 2 &&
                                sizeY > Y + 2 &&

                               (_ColorMap.map[X + 1, Y] == colorMapIndex) &&
                               (_ColorMap.map[X + 1, Y + 1] == colorMapIndex) &&
                               (_ColorMap.map[X + 2, Y + 1] == colorMapIndex) &&
                               (_ColorMap.map[X + 2, Y + 2] == colorMapIndex)
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X + 1, Y));
                                CheckList.Add(new Vector2(X + 1, Y + 1));
                                CheckList.Add(new Vector2(X + 2, Y + 1));
                                CheckList.Add(new Vector2(X + 2, Y + 2));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 1;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X + 1, Y] = -1;
                                    _ColorMap.map[X + 1, Y + 1] = -1;
                                    _ColorMap.map[X + 2, Y + 1] = -1;
                                    _ColorMap.map[X + 2, Y + 2] = -1;
                                }
                            }


                            //PT3 - R2
                            //④③
                            //  ②①
                            //    ㅍ
                            if (
                                0 <= X - 2 &&
                                sizeY > Y + 2 &&

                               (_ColorMap.map[X, Y + 1] == colorMapIndex) &&
                               (_ColorMap.map[X - 1, Y + 1] == colorMapIndex) &&
                               (_ColorMap.map[X - 1, Y + 2] == colorMapIndex) &&
                               (_ColorMap.map[X - 2, Y + 2] == colorMapIndex)
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X, Y + 1));
                                CheckList.Add(new Vector2(X - 1, Y + 1));
                                CheckList.Add(new Vector2(X - 1, Y + 2));
                                CheckList.Add(new Vector2(X - 2, Y + 2));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 2;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X, Y + 1] = -1;
                                    _ColorMap.map[X - 1, Y + 1] = -1;
                                    _ColorMap.map[X - 1, Y + 2] = -1;
                                    _ColorMap.map[X - 2, Y + 2] = -1;
                                }
                            }

                            //PT3 - R3
                            //  ①ㅍ
                            //③②
                            //④
                            if (
                                0 <= X - 2 &&
                                0 <= Y - 2 &&

                               (_ColorMap.map[X - 1, Y] == colorMapIndex) &&
                               (_ColorMap.map[X - 1, Y - 1] == colorMapIndex) &&
                               (_ColorMap.map[X - 2, Y - 1] == colorMapIndex) &&
                               (_ColorMap.map[X - 2, Y - 2] == colorMapIndex)
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X - 1, Y));
                                CheckList.Add(new Vector2(X - 1, Y - 1));
                                CheckList.Add(new Vector2(X - 2, Y - 1));
                                CheckList.Add(new Vector2(X - 2, Y - 2));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 3;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X - 1, Y] = -1;
                                    _ColorMap.map[X - 1, Y - 1] = -1;
                                    _ColorMap.map[X - 2, Y - 1] = -1;
                                    _ColorMap.map[X - 2, Y - 2] = -1;
                                }
                            }
                        }
                        // PT4
                        else if (colorMapIndex >= 15000 && colorMapIndex < 16000)
                        {
                            blockName = "PT4";
                            data += "<id>" + blockName + "</id>\n";
                            data += "<color>" + color + "</color>\n";

                            //PT3 - R0
                            //ㅍㅁㅁ
                            //ㅁ
                            //ㅁ
                            if (
                                0 <= Y - 2 &&
                                sizeX > X + 2 &&

                               (_ColorMap.map[X + 1, Y] == colorMapIndex) &&   // 1
                               (_ColorMap.map[X + 2, Y] == colorMapIndex) &&   // 2
                               (_ColorMap.map[X, Y - 1] == colorMapIndex) &&      // 3
                               (_ColorMap.map[X, Y - 2] == colorMapIndex)         // 4
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X + 1, Y));
                                CheckList.Add(new Vector2(X + 2, Y));
                                CheckList.Add(new Vector2(X, Y - 1));
                                CheckList.Add(new Vector2(X, Y - 2));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 0;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X + 1, Y] = -1;
                                    _ColorMap.map[X + 2, Y] = -1;
                                    _ColorMap.map[X, Y - 1] = -1;
                                    _ColorMap.map[X, Y - 2] = -1;
                                }
                            }

                            //PT3 - R1
                            //④
                            //③
                            //ㅍ①②
                            if (
                                sizeX > X + 2 &&
                                sizeY > Y + 2 &&

                               (_ColorMap.map[X + 1, Y] == colorMapIndex) &&   // 1
                               (_ColorMap.map[X + 2, Y] == colorMapIndex) &&   // 2
                               (_ColorMap.map[X, Y + 1] == colorMapIndex) &&   // 3
                               (_ColorMap.map[X, Y + 2] == colorMapIndex)      // 4
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X + 1, Y));
                                CheckList.Add(new Vector2(X + 2, Y));
                                CheckList.Add(new Vector2(X, Y + 1));
                                CheckList.Add(new Vector2(X, Y + 2));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 1;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X + 1, Y] = -1;
                                    _ColorMap.map[X + 2, Y] = -1;
                                    _ColorMap.map[X, Y + 1] = -1;
                                    _ColorMap.map[X, Y + 2] = -1;
                                }
                            }


                            //PT3 - R2
                            //    ④
                            //    ③
                            //①②ㅍ
                            if (
                                0 <= X - 2 &&
                                sizeY > Y + 2 &&

                                (_ColorMap.map[X - 2, Y] == colorMapIndex) &&     // 1
                                (_ColorMap.map[X - 1, Y] == colorMapIndex) &&     // 2
                                (_ColorMap.map[X, Y + 1] == colorMapIndex) &&  // 3
                                (_ColorMap.map[X, Y + 2] == colorMapIndex)     // 4
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X - 2, Y));
                                CheckList.Add(new Vector2(X - 1, Y));
                                CheckList.Add(new Vector2(X, Y + 1));
                                CheckList.Add(new Vector2(X, Y + 2));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 2;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X - 2, Y] = -1;
                                    _ColorMap.map[X - 1, Y] = -1;
                                    _ColorMap.map[X, Y + 1] = -1;
                                    _ColorMap.map[X, Y + 2] = -1;
                                }
                            }

                            //PT3 - R3
                            //①②ㅍ
                            //    ③
                            //    ④
                            if (
                                0 <= X - 2 &&
                                0 <= Y - 2 &&

                                (_ColorMap.map[X - 2, Y] == colorMapIndex) &&     // 1
                                (_ColorMap.map[X - 1, Y] == colorMapIndex) &&     // 2
                                (_ColorMap.map[X, Y - 1] == colorMapIndex) &&     // 3
                                (_ColorMap.map[X, Y - 2] == colorMapIndex)        // 4
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X - 2, Y));
                                CheckList.Add(new Vector2(X - 1, Y));
                                CheckList.Add(new Vector2(X, Y - 1));
                                CheckList.Add(new Vector2(X, Y - 2));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 3;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X - 2, Y] = -1;
                                    _ColorMap.map[X - 1, Y] = -1;
                                    _ColorMap.map[X, Y - 1] = -1;
                                    _ColorMap.map[X, Y - 2] = -1;
                                }
                            }
                        }
                        // PT5
                        else if (colorMapIndex >= 16000 && colorMapIndex < 17000)
                        {
                            blockName = "PT5";
                            data += "<id>" + blockName + "</id>\n";
                            data += "<color>" + color + "</color>\n";

                            //PT3 - R0
                            //ㅁㅍ
                            //ㅁ
                            //ㅁㅁ
                            if (
                                0 <= X - 1 &&
                                0 <= Y - 2 &&

                                (_ColorMap.map[X - 1, Y] == colorMapIndex) &&     // 1
                                (_ColorMap.map[X - 1, Y - 1] == colorMapIndex) && // 2
                                (_ColorMap.map[X - 1, Y - 2] == colorMapIndex) && // 3
                                _ColorMap.map[X, Y - 2] == colorMapIndex            // 4
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X - 1, Y));
                                CheckList.Add(new Vector2(X - 1, Y - 1));
                                CheckList.Add(new Vector2(X - 1, Y - 2));
                                CheckList.Add(new Vector2(X, Y - 2));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 0;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X - 1, Y] = -1;
                                    _ColorMap.map[X - 1, Y - 1] = -1;
                                    _ColorMap.map[X - 1, Y - 2] = -1;
                                    _ColorMap.map[X, Y - 2] = -1;
                                }
                            }

                            //PT3 - R1
                            //ㅍ  ④
                            //①②③
                            if (
                                0 <= Y - 1 &&
                                sizeX > X + 2 &&

                                _ColorMap.map[X, Y - 1] == colorMapIndex &&         // 1
                                _ColorMap.map[X + 1, Y - 1] == colorMapIndex &&  // 2
                                _ColorMap.map[X + 2, Y - 1] == colorMapIndex &&  // 3
                                _ColorMap.map[X + 2, Y] == colorMapIndex
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X, Y - 1));
                                CheckList.Add(new Vector2(X + 1, Y - 1));
                                CheckList.Add(new Vector2(X + 2, Y - 1));
                                CheckList.Add(new Vector2(X + 2, Y));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 1;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X, Y - 1] = -1;
                                    _ColorMap.map[X + 1, Y - 1] = -1;
                                    _ColorMap.map[X + 2, Y - 1] = -1;
                                    _ColorMap.map[X + 2, Y] = -1;
                                }
                            }


                            //PT3 - R2
                            //④③
                            //  ②
                            //ㅍ①
                            if (
                                sizeX > X + 1 &&
                                sizeY > Y + 2 &&

                                (_ColorMap.map[X + 1, Y] == colorMapIndex) &&      // 1
                                (_ColorMap.map[X + 1, Y + 1] == colorMapIndex) &&  // 2
                                (_ColorMap.map[X + 1, Y + 2] == colorMapIndex) &&  // 3
                                _ColorMap.map[X, Y + 2] == colorMapIndex
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X + 1, Y));
                                CheckList.Add(new Vector2(X + 1, Y + 1));
                                CheckList.Add(new Vector2(X + 1, Y + 2));
                                CheckList.Add(new Vector2(X, Y + 2));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 2;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X + 1, Y] = -1;
                                    _ColorMap.map[X + 1, Y + 1] = -1;
                                    _ColorMap.map[X + 1, Y + 2] = -1;
                                    _ColorMap.map[X, Y + 2] = -1;
                                }
                            }

                            //PT3 - R3
                            //③②①
                            //④  ㅍ
                            if (
                                0 <= X - 2 &&
                                sizeY > Y + 1 &&

                                (_ColorMap.map[X, Y + 1] == colorMapIndex) &&  // 1
                                (_ColorMap.map[X - 1, Y + 1] == colorMapIndex) && // 2
                                (_ColorMap.map[X - 2, Y + 1] == colorMapIndex) && // 3
                                _ColorMap.map[X - 2, Y] == colorMapIndex
                               )
                            {
                                bool isAvaliable = false;
                                List<Vector2> CheckList = new List<Vector2>();
                                CheckList.Add(new Vector2(X, Y + 1));
                                CheckList.Add(new Vector2(X - 1, Y + 1));
                                CheckList.Add(new Vector2(X - 2, Y + 1));
                                CheckList.Add(new Vector2(X - 2, Y));

                                // 시작되는 블록 지점부터 모든 블록이 연결되어 있으면 활성화
                                for (int n = 0; n < CheckList.Count; n++)
                                {
                                    int count = 0;
                                    for (int c = 0; c < CheckList.Count; c++)
                                    {
                                        if (n == c) continue;

                                        // 현재 블록과 다음 블록을 비교
                                        if (
                                            // 수직상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].x == CheckList[c].x) && (Mathf.Abs(CheckList[n].y - CheckList[c].y) == 1)) ||
                                            // 수평상 1칸 차이면 블록 연결 성립
                                            ((CheckList[n].y == CheckList[c].y) && (Mathf.Abs(CheckList[n].x - CheckList[c].x) == 1)))
                                        {
                                            count++;
                                        }
                                    }

                                    if (count == 0)
                                    {
                                        isAvaliable = true;
                                        break;
                                    }
                                }

                                // 블록위치가 성립하면 출력 및 설정 초기화
                                if (isAvaliable == false)
                                {
                                    rot = 3;
                                    data += "<DX>" + ((sizeY - 1) - Y) + "</DX> " + "<DY>" + X + "</DY> " + "<ROT>" + rot + "</ROT>";

                                    XML_DATA_LIST.Add(data);

                                    // 블록들 초기화
                                    _ColorMap.map[X, Y] = -1;
                                    _ColorMap.map[X, Y + 1] = -1;
                                    _ColorMap.map[X - 1, Y + 1] = -1;
                                    _ColorMap.map[X - 2, Y + 1] = -1;
                                    _ColorMap.map[X - 2, Y] = -1;
                                }
                            }
                        }
                    }
                }
            }

            Debug.Log(map);
            //Debug.Log("(1) : " + isCheckPixel_3);
            //Debug.Log("(2) : " + isCheckPixel_4);
            
            // 시간초가 초과 되었으면 그냥 출력
            if (isTimeOver == false)
            {
                string allData = "";
                for (int i = 0; i < XML_DATA_LIST.Count; i++)
                {
                    allData += XML_DATA_LIST[i] + "\n";
                }

                if (!string.IsNullOrEmpty(allData))
                {
                    Debug.Log(allData);
                }
            }
        }

        if (isTimeOver)
        {
            myResultText.text = "타임오버";
        }
        else
        {
            myResultText.text = result;
        }

        //WAITBLOCKS_WINDOW
        WaitBlockNumOfList.Clear();

        // 세트 블록 사용 여부 확인 리스트 생성
        int blockSetCount = (Option_PentoMode.isOn) ? 16 : 11;
        for (int i = 0; i < blockSetCount; i++)
        {
            int checkCount = 0;
            for (int set = 0; set < MatchToSetNum[colorIndex]; set++)
            {
                if (wbm_List[set].waitBlockCheckList[i])
                    checkCount++;
            }

            WaitBlockNumOfList.Add(checkCount);
        }

        for (int i = 0; i < blockSetCount; i++)
        {
            if(WaitBlockNumOfList[i] != 0)
            {
                WB_PANEL_ITEMS[i].gameObject.SetActive(true);
                WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text = WaitBlockNumOfList[i].ToString();
            }
            else
            {
                WB_PANEL_ITEMS[i].gameObject.SetActive(false);
                WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text = 0.ToString();
            }
        }
    }

    /// <summary>
    /// 위치 값을 받아 해당 위치부터 여러 블록들 놓을 수 있는지 확인하는 함수
    /// </summary>
    /// <param name="posListCount">선택된 블록 색상의 모든 블록의 수</param>
    /// <returns>패턴 확인 후 정답 성립 여부 반환</returns>
    // * 1. 선택된 색상의 모든 블록의 위치를 순차적 확인
    // * 2. 각 위치마다 랜덤하게 11개(펜타는 16개) 블록 중 하나를 놓아보고 맞아 들어가는지 확인
    // * 2-1. 블록을 놓을 수 있지만, 탐사해야할 블록이 남아있으면 남은 블록에 다른 불록이 들어 갈 수 있는지 확인
    // *      만약 다른 블록을 넣을 수 없으면, 정답이 성립할 수 없어서 알고리즘 중단
    // *      만약 블록을 넣을 수 있다면, 정답 성립 가능성이 있기에 2-2와 동일하게 실행
    // * 2-2. 블록을 놓을 수 없으면, 다음 다른 블록으로 확인
    // * 3. 모든 블록을 확인
    public bool ReadingPatternCheck(int posListCount)
    {
        // 타임 아웃 체크
        eTime += Time.deltaTime;
        if (eTime >= mTime)
        {
            isTimeOver = true;
            return false;
        }

        loopCount++; // 루프 카운트
        // posList리스트 = 선택된 색상의 모든 블록 수
        // poListCount = 블록 위치 중 랜덤으로 선정된 위치
        //Debug.Log("POS LIST COUNT : " + posList.Count + ", " + posListCount);
        // 현재 맵을 이전 맵으로 저장
        for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
        {
            for (int j = 0; j < MapManager.S.mapPixel_X; j++)
            {
                prev[j, i] = curr[j, i];
            }
        }

        // AM1 카운터
        int monoCount       = 0;
        int remainMonoCount = 0;

        // 맵에 존재하는 각 블록의 수만큼 반복 (4개가 존재하면 4번 반복)
        // 혼자 다른 블록과 이어지지 않는 블록을 찾는 구문
        for (int item = 0; item < posList.Count; item++)
        {
            // x, y 좌표 확인 (0, 0)부터 시작
            int x = (int)posList[item].x;
            int y = (int)posList[item].y;

            // 지정 부분 색상 확인
            // 현재 선택된 색상과 같으면 실행
            if (curr[x, y] == colorIndex)
            {
                // 맵을 나가지 않는 범위에서 사방향이 색상이 맞지 않으면 AM1카운터 증가
                // 혼자 갇힌 블록 찾는 문구
                if(y + 1 <  sizeY && curr[x, y + 1] != colorIndex &&
                   y - 1 >= 0     && curr[x, y - 1] != colorIndex &&
                   x + 1 < sizeX  && curr[x + 1, y] != colorIndex &&
                   x - 1 >= 0     && curr[x - 1, y] != colorIndex )
                {
                    monoCount++;
                }
            }
        }

        // 세트에 남은 AM1 수 확인
        for (int i = 0; i < MatchToSetNum[colorIndex]; i++)
        {
            if (wbm_List[i].waitBlockCheckList[0] == false)
            {
                remainMonoCount++;
            }
        }

        // 필요한 AM1이 사용가능한 AM1 수보다 많으면 세트 부족으로 취소
        if (monoCount > remainMonoCount) return false;

        // 확인해야하는 블록 위치 확인
        int X = (int)posList[posListCount].x;
        int Y = (int)posList[posListCount].y;

        // 블록 순서를 랜덤으로 저장하는 리스트. FastMode에서는 순차적 입력
        List<int> randomValueList = new List<int>();

        /* 중요 */
        // - 맵에 놓여질 블록과 순서를 결정하는 구문
        // Fast 모드가 아니면 실행
        // 탐색 순서를 랜덤으로 지정하여 나열
        if(Option_FastMode.isOn == false)
        {
            // 블록 수만큼 정렬
            List<int> prevRandomWindowList;

            // 11개 블록 탐색
            if (!Option_PentoMode.isOn)
            {
                prevRandomWindowList = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
            }
            // 펜토미노 모드일때, 펜토미노까지 검색에 입력
            else
            {
                prevRandomWindowList = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };

                // 펜토미노 블록 입력
                for (int i = 11; i < 16; i++)
                {
                    int rValue = Random.Range(0, prevRandomWindowList.Count);
                    randomValueList.Add(prevRandomWindowList[rValue]);

                    prevRandomWindowList.RemoveAt(rValue);
                }
            }

            // AM1이 필요 없으면 0번 지움
            if (!Option_MonoMode.isOn) prevRandomWindowList.RemoveAt(0);

            int maxCount = prevRandomWindowList.Count;
            // TE1~TE7까지 블록 중 랜덤으로 선택하여 입력
            for (int i = 4; i < maxCount; i++)
            {
                int rValue = Random.Range(4, prevRandomWindowList.Count);
                randomValueList.Add(prevRandomWindowList[rValue]);

                prevRandomWindowList.RemoveAt(rValue);
            }
            // AM1~TR2까지 블록 중 랜덤으로 선택하여 입력
            for (int i = 0; i < 4; i++)
            {
                int rValue = Random.Range(0, prevRandomWindowList.Count);
                randomValueList.Add(prevRandomWindowList[rValue]);

                prevRandomWindowList.RemoveAt(rValue);
            }
        }
        // Fast 모드이면 실행
        // 탐색 순서를 순차적으로 나열
        else
        {
            // 무작위로 블록 입력
            // 펜토미노 모드이면 15개 블록 확인
            int i = (Option_PentoMode.isOn)? 15 : 10;
            for (; i >= 0; i--)
            {
                randomValueList.Add(i);
            }
        }

        // * 중요 *
        // 세트 수 만큼 반복
        // 사용 예시 : case 9의 TE6 - R0 확인
        for (int set = 0; set < MatchToSetNum[colorIndex]; set++)
        {
            // 블록 수만큼 반복
            for (int val = 0; val < randomValueList.Count; val++)
            {
                switch (randomValueList[val])
                {
                    case 0: // AM1
                        //ㅁ
                        {
                            if ((isCheckPixel_4 && isCheckPixel_3) == false) break;
                            //AM1 - ok (complete)
                            if (wbm_List[set].waitBlockCheckList[0] == false)
                            {
                                //Debug.Log("0==========================");

                                int num = 1000 + set;

                                //Debug.Log("AM1");
                                curr[X, Y] = num;
                                wbm_List[set].waitBlockCheckList[0] = true;

                                int nextCount = posListCount + 1;
                                bool isCheck = false;

                                for (int i = 0; i < posList.Count; i++)
                                {
                                    if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                    {
                                        nextCount = i;
                                        isCheck = true;
                                        break;
                                    }
                                }

                                ////Debug.Log("NextCount : " + nextCount);

                                if (isCheck == false)
                                {
                                    //Debug.Log("만세 클리어!");
                                    return true;
                                }

                                if (ReadingPatternCheck(nextCount))
                                {
                                    return true;
                                }
                                else
                                {
                                    wbm_List[set].waitBlockCheckList[0] = false;
                                    curr[X, Y] = colorIndex;
                                }
                            }
                            break;
                        }

                    case 1: // D1
                        //ㅁㅁ
                        {
                            if ((isCheckPixel_4 && isCheckPixel_3) == false) break;
                            //D1 - ok (complete)
                            if (wbm_List[set].waitBlockCheckList[1] == false)
                            {
                                ////Debug.Log("1==========================");

                                int num = 2000 + set;

                                //D1 - R0
                                if ((sizeX > X + 1 && curr[X + 1, Y] == colorIndex))
                                {
                                    ////Debug.Log("D1 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    wbm_List[set].waitBlockCheckList[1] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[1] = false;
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                    }
                                }

                                //D1 - R1
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("D1 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[1] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[1] = false;
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }

                                //D1 - R2
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex))
                                {
                                    ////Debug.Log("D1 - R2");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    wbm_List[set].waitBlockCheckList[1] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[1] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                    }
                                }

                                //D1 - R3
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex))
                                {
                                    //Debug.Log("D1 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[1] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[1] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                    }
                                }
                            }
                            break;
                        }

                    case 2: // TR1
                        //ㅁㅁㅁ
                        {
                            if (((isCheckPixel_4 && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;
                            //TR1 - ok (complete)
                            if (wbm_List[set].waitBlockCheckList[2] == false)
                            {
                                //Debug.Log("2==========================");

                                int num = 3000 + set;

                                //A~~~~~~~~~~~~~~~~~~
                                //TR1 - R0
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y] == colorIndex))
                                {
                                    ////Debug.Log("TR1 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    wbm_List[set].waitBlockCheckList[2] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[2] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                    }
                                }

                                //TR1 - R1
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X, Y - 2] == colorIndex))
                                {
                                    ////Debug.Log("TR1 - R1");

                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    wbm_List[set].waitBlockCheckList[2] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[2] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }

                                //TR1 - R2
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y] == colorIndex))
                                {
                                    ////Debug.Log("TR1 - R2");

                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    wbm_List[set].waitBlockCheckList[2] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[2] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                    }
                                }

                                //TR1 - R3
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X, Y + 2] == colorIndex))
                                {
                                    //Debug.Log("TR1 - R3");

                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    wbm_List[set].waitBlockCheckList[2] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[2] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }

                                //B~~~~~~~~~~~~~~~~~~~
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex))
                                {
                                    ////Debug.Log("TR1 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X - 1, Y] = num;
                                    wbm_List[set].waitBlockCheckList[2] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[2] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                    }
                                }

                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex))
                                {
                                    //Debug.Log("TR1 - R0");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[2] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[2] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }
                            }
                            break;
                        }

                    case 3: // TR2
                        //ㅁㅁ
                        //  ㅁ
                        {

                            if (((isCheckPixel_4 && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;
                            //TR2 - ok (complete)
                            if (wbm_List[set].waitBlockCheckList[3] == false)
                            {
                                //Debug.Log("3==========================");

                                int num = 4000 + set;

                                //A~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                //TR2 - R0
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X + 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TR2 - R1
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TR2 - R2
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X - 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R2");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                    }
                                }

                                //TR2 - R3
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                    }
                                }

                                //B~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R0");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }

                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R0");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                    }
                                }

                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                    }
                                }

                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }

                                //C~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R0");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                    }
                                }

                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X + 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                    }
                                }

                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R0");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X - 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TR2 - R0");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[3] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[3] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }
                            }
                            break;
                        }

                    case 4: // TE1
                        //ㅁㅁㅁㅁ
                        {
                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;
                            //TE1 - ok (complete)
                            if (wbm_List[set].waitBlockCheckList[4] == false)
                            {
                                ////Debug.Log("4==========================");

                                int num = 5000 + set;

                                //A~~~~~~~~~~~~~~
                                //TE1 - R0
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y] == colorIndex) &&
                                   (sizeX > X + 3 && curr[X + 3, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE1 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X + 3, Y] = num;
                                    wbm_List[set].waitBlockCheckList[4] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[4] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X + 3, Y] = colorIndex;
                                    }
                                }

                                //TE1 - R1
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X, Y + 2] == colorIndex) &&
                                   (sizeY > Y + 3 && curr[X, Y + 3] == colorIndex))
                                {
                                    ////Debug.Log("TE1 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    curr[X, Y + 3] = num;
                                    wbm_List[set].waitBlockCheckList[4] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[4] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                        curr[X, Y + 3] = colorIndex;
                                    }
                                }

                                //TE1 - R2
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y] == colorIndex) &&
                                   (0 <= X - 3 && curr[X - 3, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE1 - R2");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 3, Y] = num;
                                    wbm_List[set].waitBlockCheckList[4] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[4] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 3, Y] = colorIndex;
                                    }
                                }

                                //TE1 - R3
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X, Y - 2] == colorIndex) &&
                                   (0 <= Y - 3 && curr[X, Y - 3] == colorIndex))
                                {
                                    ////Debug.Log("TE1 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    curr[X, Y - 3] = num;
                                    wbm_List[set].waitBlockCheckList[4] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[4] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                        curr[X, Y - 3] = colorIndex;
                                    }
                                }
                            }
                            break;
                        }

                    case 5: // TE2
                        //ㅁㅁㅁ
                        //    ㅁ
                        {

                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;
                            //TE2 - ok (complete) + (plusTemplete)
                            if (wbm_List[set].waitBlockCheckList[5] == false)
                            {
                                ////Debug.Log("5==========================");
                                int num = 6000 + set;

                                //A~~~~~~~~~~~~~~~~
                                //TE2 - R0
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X + 2, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X + 2, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                    }
                                }

                                //TE2 - R1
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X, Y + 2] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y + 2] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    curr[X + 1, Y + 2] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                    }
                                }

                                //TE2 - R2
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X - 2, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R2");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 2, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                    }
                                }

                                //TE2 - R3
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X, Y - 2] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y - 2] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    curr[X - 1, Y - 2] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                    }
                                }

                                //B~~~~~~~~~~~~~~~~
                                //TE2 - R0
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X + 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R0");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE2 - R1
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                    }
                                }

                                //TE2 - R2
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X - 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R2");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                    }
                                }

                                //TE2 - R3
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }

                                //C~~~~~~~~~~~~~~~~
                                //TE2 - R0
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R0");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                    }
                                }

                                //TE2 - R1
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X, Y - 2] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R1");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }

                                //TE2 - R2
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R2");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                    }
                                }

                                //TE2 - R3
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X, Y + 2] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R3");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }

                                //D~~~~~~~~~~~~~~~~
                                //TE2 - R0
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y + 1] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R0");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 2, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                    }
                                }

                                //TE2 - R1
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X - 1, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X - 1, Y - 2] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R1");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 1, Y - 2] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                    }
                                }

                                //TE2 - R2
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y - 1] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R2");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 2, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                    }
                                }

                                //TE2 - R3
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X + 1, Y + 2] == colorIndex))
                                {
                                    ////Debug.Log("TE2 - R3");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 1, Y + 2] = num;
                                    wbm_List[set].waitBlockCheckList[5] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[5] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                    }
                                }
                            }
                            break;
                        }

                    case 6: // TE3
                        //ㅁㅁㅁ
                        //ㅁ
                        {
                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;
                            //TE3 - ok (complete) + (plusTemplete)
                            if (wbm_List[set].waitBlockCheckList[6] == false)
                            {
                                ////Debug.Log("6==========================");
                                int num = 7000 + set;

                                //A~~~~~~~~~~~~~~~~~~~~~~~
                                //TE3 - R0
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y + 1] == colorIndex)
                                   )
                                {
                                    ////Debug.Log("TE3 - R0");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 2, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                    }
                                }

                                //TE3 - R1
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X - 1, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X - 1, Y + 2] == colorIndex))
                                {
                                    ////Debug.Log("TE3 - R1");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 1, Y + 2] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                    }
                                }

                                //TE3 - R2
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y - 1] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y - 1] == colorIndex))
                                {
                                    //Debug.Log("TE3 - R2");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 2, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 2, Y - 1] = colorIndex;
                                    }
                                }

                                //TE3 - R3
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X + 1, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X + 1, Y - 2] == colorIndex))
                                {
                                    ////Debug.Log("TE3 - R3");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 1, Y - 2] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                    }
                                }

                                //B~~~~~~~~~~~~~~~~~~~~~~~
                                //TE3 - R0
                                //ㅍㅁㅁ
                                //ㅁ
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE3 - R0");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                    }
                                }

                                //TE3 - R1
                                //ㅁ
                                //ㅁ
                                //ㅍㅁ
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X, Y + 2] == colorIndex)
                                   )
                                {
                                    ////Debug.Log("TE3 - R1");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }

                                //TE3 - R2
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y] == colorIndex))
                                {
                                    //Debug.Log("TE3 - R2");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                    }
                                }

                                //TE3 - R3
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X, Y - 2] == colorIndex))
                                {
                                    ////Debug.Log("TE3 - R3");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }

                                //C~~~~~~~~~~~~~~~~~~~~~~~
                                //TE3 - R0
                                //ㅁㅍㅁ
                                //ㅁ
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X - 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE3 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE3 - R1
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE3 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE3 - R2
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X + 1, Y + 1] == colorIndex))
                                {
                                    //Debug.Log("TE3 - R2");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                    }
                                }

                                //TE3 - R3
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE3 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                    }
                                }

                                //D~~~~~~~~~~~~~~~~~~~~~~~
                                //TE3 - R0
                                //ㅁㅁㅍ
                                //ㅁ
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X - 2, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE3 - R0");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 2, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 2, Y - 1] = colorIndex;
                                    }
                                }

                                //TE3 - R1
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X, Y - 2] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y - 2] == colorIndex))
                                {
                                    ////Debug.Log("TE3 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    curr[X + 1, Y - 2] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                    }
                                }

                                //TE3 - R2
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X + 2, Y + 1] == colorIndex))
                                {
                                    //Debug.Log("TE3 - R2");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X + 2, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                    }
                                }

                                //TE3 - R3
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X, Y + 2] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y + 2] == colorIndex))
                                {
                                    ////Debug.Log("TE3 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    curr[X - 1, Y + 2] = num;
                                    wbm_List[set].waitBlockCheckList[6] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[6] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                    }
                                }
                            }
                            break;
                        }

                    case 7: // TE4
                        //ㅁㅁ
                        //ㅁㅁ
                        {
                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;
                            //TE4 - ok (complete) + (plusTemplete) - pass
                            if (wbm_List[set].waitBlockCheckList[7] == false)
                            {
                                ////Debug.Log("7==========================");
                                int num = 8000 + set;

                                //TE4 - R0
                                //ㅁㅁ
                                //ㅍㅁ
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (curr[X, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE4 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[7] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[7] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                    }
                                }

                                //TE4 - R1
                                //ㅁㅁ
                                //ㅁㅍ
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y + 1] == colorIndex) &&
                                   (curr[X - 1, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE4 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 1, Y] = num;
                                    wbm_List[set].waitBlockCheckList[7] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[7] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                    }
                                }

                                //TE4 - R2
                                //ㅁㅍ
                                //ㅁㅁ
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X - 1, Y - 1] == colorIndex) &&
                                   (curr[X, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE4 - R2");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[7] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[7] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }

                                //TE4 - R3
                                //ㅍㅁ
                                //ㅁㅁ
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X + 1, Y - 1] == colorIndex) &&
                                   (curr[X, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE4 - R3");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[7] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[7] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }
                            }
                            break;
                        }

                    case 8: // TE5
                        //  ㅁㅁ
                        //ㅁㅁ
                        {
                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;
                            //TE5 - ok (complete) + (plusTemplete)
                            if (wbm_List[set].waitBlockCheckList[8] == false)
                            {
                                ////Debug.Log("8==========================");
                                int num = 9000 + set;

                                //A~~~~~~~~~~~~~~~~~~
                                //  ㅁㅁ
                                //ㅍㅁ
                                #region TE5 - 1
                                //TE5 - R0
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 2, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                    }
                                }

                                //TE5 - R1
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X - 1, Y + 2] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 1, Y + 2] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                    }
                                }

                                //TE5 - R2
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X - 1, Y - 1] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R2");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 2, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 2, Y - 1] = colorIndex;
                                    }
                                }

                                //TE5 - R3
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (sizeY > X + 1 && curr[X + 1, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X + 1, Y - 2] == colorIndex))
                                {
                                    //Debug.Log("TE5 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 1, Y - 2] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                    }
                                }
                                #endregion

                                //B~~~~~~~~~~~~~~~~~~
                                //  ㅁㅁ
                                //ㅁㅍ
                                #region TE5 - 2
                                //TE5 - R0
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R0");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                    }
                                }

                                //TE5 - R1
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X - 1, Y + 1] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R1");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }

                                //TE5 - R2
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R2");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE5 - R3
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X + 1, Y - 1] == colorIndex))
                                {
                                    //Debug.Log("TE5 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }
                                #endregion

                                //C~~~~~~~~~~~~~~~~~~
                                //  ㅍㅁ
                                //ㅁㅁ
                                #region TE5 - 3
                                //TE5 - R0
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE5 - R1
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X + 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE5 - R2
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R2");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X - 1, Y] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                    }
                                }

                                //TE5 - R3
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X - 1, Y + 1] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex))
                                {
                                    //Debug.Log("TE5 - R3");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }
                                #endregion

                                //D~~~~~~~~~~~~~~~~~~
                                //  ㅁㅍ
                                //ㅁㅁ
                                #region TE5 - 4
                                //TE5 - R0
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X - 1, Y - 1] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R0");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 2, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 2, Y - 1] = colorIndex;
                                    }
                                }

                                //TE5 - R1
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X + 1, Y - 2] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 1, Y - 2] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                    }
                                }

                                //TE5 - R2
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE5 - R2");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 2, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                    }
                                }

                                //TE5 - R3
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X - 1, Y + 2] == colorIndex))
                                {
                                    //Debug.Log("TE5 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 1, Y + 2] = num;
                                    wbm_List[set].waitBlockCheckList[8] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[8] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                    }
                                }
                                #endregion
                            }
                            break;
                        }

                    case 9: // TE6
                        //ㅁㅁㅁ
                        //  ㅁ
                        {
                            // ㅍ : 피봇, ㅁ : 블록

                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;
                            //TE6 - ok (complete) + (plusTemplete)
                            if (wbm_List[set].waitBlockCheckList[9] == false)
                            {
                                // 여러개의 TE6이 존재할때, 각 TE6을 구분하기 위한 int 변수 선언
                                // * 2세트 사용가능 : 10001, 10002의 TE6이 존재
                                int num = 10000 + set;

                                //A~~~~~~~~~~~~~~~
                                //ㅍㅁㅁ
                                //  ㅁ
                                #region TE6 - 1
                                //TE6 - R0
                                //ㅍㅁㅁ
                                //  ㅁ
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X + 1, Y - 1] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R0");
                                    // 현재 맵에서 찾은 부위 블록값 변경
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 2, Y] = num;
                                    // 블록 사용 체크
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1; // 현재 위치 다음 블록부터 검사
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록만 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // Debug.Log("NextCount : " + nextCount);

                                    // 더이상 남은 색상의 블록이 없으면 실행
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 탐색할 블록이 남아있으면 해당 색상 부분부터 어떤 도형을 놓을 수 있는지 탐색
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 남은 부분들에  어떠한 도형도 놓을 수 없으면
                                    // 퍼즐 정답이 성립되지 않아서 다시 탐색을 시작하게 만든다.
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                    }
                                }

                                //TE6 - R1
                                //ㅁ
                                //ㅁㅁ
                                //ㅍ
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X, Y + 2] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    curr[X + 1, Y + 1] = num;
                                    // 블록 사용 체크
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1; // 랜덤 위치 다음 블록부터 검사
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }
                                    

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                    }
                                }

                                //TE6 - R2
                                //  ㅁ
                                //ㅁㅁㅍ
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X - 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R2");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;
                                    Debug.Log("NextCount : " + nextCount);
                                    Debug.Log("PosListCount : " + posListCount);

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    Debug.Log("NextCount After : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                    }
                                }

                                //TE6 - R3
                                //  ㅍ
                                //ㅁㅁ
                                //  ㅁ
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X, Y - 2] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    curr[X - 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;
                                    
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }
                                #endregion

                                //B~~~~~~~~~~~~~~~
                                //ㅁㅍㅁ
                                //  ㅁ
                                #region TE6 - 2
                                //TE6 - R0
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R0");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }

                                //TE6 - R1
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                    }
                                }

                                //TE6 - R2
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R2");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X - 1, Y] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                    }
                                }

                                //TE6 - R3
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                    }
                                }
                                #endregion

                                //C~~~~~~~~~~~~~~~
                                //ㅁㅁㅍ
                                //  ㅁ
                                #region TE6 - 2
                                //TE6 - R0
                                //ㅁㅁㅍ
                                //  ㅁ
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X - 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R0");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE6 - R1
                                //ㅍ
                                //ㅁㅁ
                                //ㅁ
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X, Y - 2] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    curr[X + 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE6 - R2
                                //  ㅁ
                                //ㅍㅁㅁ
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X + 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R2");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                    }
                                }

                                //TE6 - R3
                                //  ㅁ
                                //ㅁㅁ
                                //  ㅍ
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X, Y + 2] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    curr[X - 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                    }
                                }
                                #endregion
                                //D~~~~~~~~~~~~~~~
                                //ㅁㅁㅁ
                                //  ㅍ
                                #region TE6 - 3
                                //TE6 - R0
                                //ㅁㅁㅁ
                                //  ㅍ
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R0");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                    }
                                }

                                //TE6 - R1
                                //ㅁ
                                //ㅁㅍ
                                //ㅁ
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X - 1, Y + 1] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X - 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R1");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE6 - R2
                                //  ㅍ
                                //ㅁㅁㅁ
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y - 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R2");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE6 - R3
                                //  ㅁ
                                //ㅍㅁ
                                //  ㅁ
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X + 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE6 - R3");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[9] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[9] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }
                                #endregion
                            }
                            break;
                        }

                    case 10: // TE7
                        //ㅁㅁ
                        //  ㅁㅁ
                        {
                            // ㅍ : 피봇, ㅁ : 블록

                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;
                            //TE7 - ok (complete) + (plusTemplete)
                            // 세트의 TE7 확인
                            if (wbm_List[set].waitBlockCheckList[10] == false)
                            {
                                ////Debug.Log("10==========================");
                                // (11000 + 세트순번) 오브젝트 네이밍 세팅
                                int num = 11000 + set;

                                //A ~~~~~~~~~~~~~~~~~~~~~~~~
                                //TE7 - R0
                                //ㅁㅁ
                                //  ㅁㅁ
                                #region TE7 - 1
                                //ㅁㅁ
                                //  ㅁㅍ
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X - 1, Y + 1] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R0");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 2, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                    }
                                }

                                //TE7 - R1
                                //  ㅍ
                                //ㅁㅁ
                                //ㅁ
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X - 1, Y - 2] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 1, Y - 2] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                    }
                                }

                                //TE7 - R2
                                //ㅍㅁ
                                //  ㅁㅁ
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X + 1, Y - 1] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R2");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 2, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                    }
                                }

                                //TE7 - R3
                                //  ㅁ
                                //ㅁㅁ
                                //ㅍ
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X + 1, Y + 2] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 1, Y + 2] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                    }
                                }
                                #endregion

                                //B ~~~~~~~~~~~~~~~~~~~~~~~~
                                //TE7 - R0
                                //ㅁㅁ
                                //  ㅍㅁ 
                                #region TE7 - 2
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                    }
                                }

                                //TE7 - R1
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X - 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE7 - R2
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R2");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE7 - R3
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R3");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }
                                #endregion

                                //C ~~~~~~~~~~~~~~~~~~~~~~~~
                                //TE7 - R0
                                //ㅁㅍ
                                //  ㅁㅁ 
                                #region TE7 - 2
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R0");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                //TE7 - R1
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R1");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }

                                //TE7 - R2
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R2");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X + 1, Y] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                    }
                                }

                                //TE7 - R3
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X - 1, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }
                                #endregion

                                //D ~~~~~~~~~~~~~~~~~~~~~~~~
                                //TE7 - R0
                                //ㅍㅁ
                                //  ㅁㅁ 
                                #region TE7 - 3
                                if (
                                   (sizeX > X + 1 && curr[X + 1, Y] == colorIndex) &&
                                   (0 <= Y - 1 && curr[X + 1, Y - 1] == colorIndex) &&
                                   (sizeX > X + 2 && curr[X + 2, Y - 1] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R0");
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 2, Y - 1] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                    }
                                }

                                //TE7 - R1
                                if (
                                   (sizeY > Y + 1 && curr[X, Y + 1] == colorIndex) &&
                                   (sizeX > X + 1 && curr[X + 1, Y + 1] == colorIndex) &&
                                   (sizeY > Y + 2 && curr[X + 1, Y + 2] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R1");
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 1, Y + 2] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                    }
                                }

                                //TE7 - R2
                                if (
                                   (0 <= X - 1 && curr[X - 1, Y] == colorIndex) &&
                                   (sizeY > Y + 1 && curr[X - 1, Y + 1] == colorIndex) &&
                                   (0 <= X - 2 && curr[X - 2, Y + 1] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R2");
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 2, Y + 1] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                    }
                                }

                                //TE7 - R3
                                if (
                                   (0 <= Y - 1 && curr[X, Y - 1] == colorIndex) &&
                                   (0 <= X - 1 && curr[X - 1, Y - 1] == colorIndex) &&
                                   (0 <= Y - 2 && curr[X - 1, Y - 2] == colorIndex))
                                {
                                    ////Debug.Log("TE7 - R3");
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 1, Y - 2] = num;
                                    wbm_List[set].waitBlockCheckList[10] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    ////Debug.Log("NextCount : " + nextCount);

                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    else
                                    {
                                        wbm_List[set].waitBlockCheckList[10] = false;

                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                    }
                                }
                                #endregion
                            }
                            break;
                        }

                    case 11: // PT1
                        //ㅁㅁㅁ
                        //  ㅁ
                        //  ㅁ
                        {
                            if (!Option_PentoMode.isOn) break;

                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;

                            if (wbm_List[set].waitBlockCheckList[15] == false)
                            {
                                // (12000 + 세트순번) 오브젝트 네이밍 세팅
                                int num = 12000 + set;

                                //A ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅍ①②
                                //  ③
                                //  ④
                                #region PT1 - 1
                                //ㅍ①②
                                //  ③
                                //  ④
                                if (
                                    0 <= Y - 2 &&
                                    sizeX > X + 2 &&

                                   ( curr[X + 1, Y] == colorIndex) &&   // 1 오른쪽
                                   ( curr[X + 2, Y] == colorIndex) &&   // 2 오른쪽
                                   ( curr[X + 1, Y - 1] == colorIndex) &&  // 3 중단 아래
                                   ( curr[X + 1, Y - 2] == colorIndex)     // 4 중단 아래
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 1, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                    }
                                }

                                //④
                                //③①②
                                //ㅍ
                                if (
                                    sizeX > X + 2 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X + 1, Y + 1] == colorIndex) &&   // 1 오른쪽
                                   ( curr[X + 2, Y + 1] == colorIndex) &&   // 2 오른쪽
                                   ( curr[X, Y + 1] == colorIndex) &&       // 3 위
                                   ( curr[X, Y + 2] == colorIndex)          // 4 위
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 2, Y + 1] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }

                                //  ④
                                //  ③
                                //①②ㅍ
                                if (
                                    0 <= X - 2 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X - 1, Y] == colorIndex) &&          // 1 왼쪽
                                   ( curr[X - 2, Y] == colorIndex) &&          // 2 왼쪽
                                   ( curr[X - 1, Y + 1] == colorIndex) &&   // 3 중단 위
                                   ( curr[X - 1, Y + 2] == colorIndex)      // 4 중단 위
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 1, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                    }
                                }

                                //    ㅍ
                                //①②③
                                //    ④
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 2 &&
                                    
                                   ( curr[X - 1, Y - 1] == colorIndex) &&  // 1 왼쪽
                                   ( curr[X - 2, Y - 1] == colorIndex) &&  // 2 왼쪽
                                   ( curr[X, Y - 1] == colorIndex) &&      // 3 아래
                                   ( curr[X, Y - 2] == colorIndex)         // 4 아래
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 2, Y - 1] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 2, Y - 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }
                                #endregion

                                //B ~~~~~~~~~~~~~~~~~~~~~~~~
                                //①ㅍ②
                                //  ③
                                //  ④
                                #region PT1 - 2
                                //①ㅍ②
                                //  ③
                                //  ④
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 2 &&
                                    sizeX > X + 1 &&

                                   (curr[X - 1, Y] == colorIndex) &&      // 1 왼쪽
                                   ( curr[X + 1, Y] == colorIndex) &&   // 2 오른쪽
                                   ( curr[X, Y - 1] == colorIndex) &&      // 3 중단 아래
                                   ( curr[X, Y - 2] == colorIndex)         // 4 중단 아래
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }

                                //③
                                //ㅍ①②
                                //④
                                if (
                                    0 <= Y - 1 &&
                                    sizeX > X + 2 &&
                                    sizeY > Y + 1 &&

                                   ( curr[X + 1, Y] == colorIndex) &&   // 1 오른쪽 
                                   ( curr[X + 2, Y] == colorIndex) &&   // 2 오른쪽
                                   ( curr[X, Y + 1] == colorIndex) &&   // 3 위
                                   ( curr[X, Y - 1] == colorIndex)         // 4 아래
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }

                                //  ④
                                //  ③
                                //①ㅍ②
                                if (
                                    0 <= X - 1 &&
                                    sizeX > X + 1 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X - 1, Y] == colorIndex) &&      // 1 왼쪽 
                                   ( curr[X + 1, Y] == colorIndex) &&   // 2 오른쪽 
                                   ( curr[X, Y + 1] == colorIndex) &&   // 3 중단 위
                                   ( curr[X, Y + 2] == colorIndex)      // 4 중단 위
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }

                                //    ③
                                //①②ㅍ
                                //    ④
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 1 &&
                                    sizeY > Y + 1 &&

                                   ( curr[X - 1, Y] == colorIndex) &&      // 1 왼쪽
                                   ( curr[X - 2, Y] == colorIndex) &&      // 2 왼쪽
                                   ( curr[X, Y + 1] == colorIndex) &&   // 3 위
                                   ( curr[X, Y - 1] == colorIndex)         // 4 아래
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }
                                #endregion

                                //C ~~~~~~~~~~~~~~~~~~~~~~~~
                                //①②ㅍ
                                //  ③
                                //  ④
                                #region PT1 - 3
                                //①②ㅍ
                                //  ③
                                //  ④
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 2 &&

                                   ( curr[X - 1, Y] == colorIndex) &&      // 1 왼쪽      
                                   ( curr[X - 2, Y] == colorIndex) &&      // 2 왼쪽      
                                   ( curr[X - 1, Y - 1] == colorIndex) &&  // 3 중단 아래 
                                   ( curr[X - 1, Y - 2] == colorIndex)     // 4 중단 아래 
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 1, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                    }
                                }

                                //ㅍ
                                //③①②
                                //④
                                if (
                                    0 <= Y - 2 &&
                                    sizeX > X + 2 &&

                                   ( curr[X + 1, Y - 1] == colorIndex) &&   // 1 오른쪽
                                   ( curr[X + 2, Y - 1] == colorIndex) &&   // 2 오른쪽
                                   ( curr[X, Y - 1] == colorIndex) &&          // 3 중단 아래
                                   ( curr[X, Y - 2] == colorIndex)             // 4 중단 아래
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 2, Y - 1] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }

                                //  ④
                                //  ③
                                //ㅍ①②
                                if (
                                    sizeX > X + 2 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X + 1, Y] == colorIndex) &&       // 1 오른쪽 
                                   ( curr[X + 2, Y] == colorIndex) &&       // 2 오른쪽 
                                   ( curr[X + 1, Y + 1] == colorIndex) &&   // 3 중단 위
                                   ( curr[X + 1, Y + 2] == colorIndex)      // 4 중단 위
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 1, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                    }
                                }

                                //    ㅁ
                                //ㅁㅁㅁ
                                //    ㅍ
                                if (
                                    0 <= X - 2 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X - 1, Y + 1] == colorIndex) &&  // 1 오른쪽
                                   ( curr[X - 2, Y + 1] == colorIndex) &&  // 2 오른쪽
                                   ( curr[X, Y + 1] == colorIndex) &&   // 3 중단 위
                                   ( curr[X, Y + 2] == colorIndex)      // 4 중단 위
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 2, Y + 1] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }
                                #endregion

                                //D ~~~~~~~~~~~~~~~~~~~~~~~~
                                //①②③
                                //  ㅍ
                                //  ④
                                #region PT1 - 4
                                //①②③
                                //  ㅍ
                                //  ④
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 1 &&
                                    sizeX > X + 1 &&
                                    sizeY > Y + 1)
                                {
                                    if (
                                       (curr[X - 1, Y + 1] == colorIndex) &&      // 왼쪽 위 
                                       (curr[X + 1, Y + 1] == colorIndex) &&   // 중단 위 
                                       (curr[X, Y + 1] == colorIndex) &&       // 오른쪽 위 
                                       (curr[X, Y - 1] == colorIndex)             // 중단 아래 
                                       )
                                    {
                                        // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                        curr[X, Y] = num;
                                        curr[X - 1, Y + 1] = num;
                                        curr[X + 1, Y + 1] = num;
                                        curr[X, Y + 1] = num;
                                        curr[X, Y - 1] = num;
                                        // 블록 상태를 사용으로 전환
                                        wbm_List[set].waitBlockCheckList[15] = true;

                                        int nextCount = posListCount + 1;
                                        bool isCheck = false;

                                        // 현재 지정된 색상의 블록 수만큼 반복
                                        // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                        // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                        //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                        for (int i = 0; i < posList.Count; i++)
                                        {
                                            if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                            {
                                                nextCount = i;
                                                isCheck = true;
                                                break;
                                            }
                                        }

                                        // 아직 남은 색상이 존재하면 다시 체크
                                        if (isCheck == false)
                                        {
                                            //Debug.Log("만세 클리어!");
                                            return true;
                                        }

                                        // 남은 부분이 블록으로 커버가 가능하면 리턴
                                        if (ReadingPatternCheck(nextCount))
                                        {
                                            return true;
                                        }
                                        // 커버가 불가능하면 정답 성립 불가능
                                        else
                                        {
                                            // 사용했던 블록 초기화
                                            wbm_List[set].waitBlockCheckList[15] = false;

                                            // 블록 값 초기화
                                            curr[X, Y] = colorIndex;
                                            curr[X - 1, Y + 1] = colorIndex;
                                            curr[X + 1, Y + 1] = colorIndex;
                                            curr[X, Y + 1] = colorIndex;
                                            curr[X, Y - 1] = colorIndex;
                                        }
                                    }

                                    //③
                                    //①ㅍ②
                                    //④
                                    if (
                                       (curr[X - 1, Y] == colorIndex) &&          // 1 왼쪽
                                       (curr[X + 1, Y] == colorIndex) &&       // 2 오른쪽 
                                       (curr[X - 1, Y + 1] == colorIndex) &&   // 3 왼쪽 위 
                                       (curr[X - 1, Y - 1] == colorIndex)         // 4 왼쪽 아래 
                                       )
                                    {
                                        // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                        curr[X, Y] = num;
                                        curr[X - 1, Y] = num;
                                        curr[X + 1, Y] = num;
                                        curr[X - 1, Y + 1] = num;
                                        curr[X - 1, Y - 1] = num;
                                        // 블록 상태를 사용으로 전환
                                        wbm_List[set].waitBlockCheckList[15] = true;

                                        int nextCount = posListCount + 1;
                                        bool isCheck = false;

                                        // 현재 지정된 색상의 블록 수만큼 반복
                                        // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                        // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                        //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                        for (int i = 0; i < posList.Count; i++)
                                        {
                                            if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                            {
                                                nextCount = i;
                                                isCheck = true;
                                                break;
                                            }
                                        }

                                        // 아직 남은 색상이 존재하면 다시 체크
                                        if (isCheck == false)
                                        {
                                            //Debug.Log("만세 클리어!");
                                            return true;
                                        }

                                        // 남은 부분이 블록으로 커버가 가능하면 리턴
                                        if (ReadingPatternCheck(nextCount))
                                        {
                                            return true;
                                        }
                                        // 커버가 불가능하면 정답 성립 불가능
                                        else
                                        {
                                            // 사용했던 블록 초기화
                                            wbm_List[set].waitBlockCheckList[15] = false;

                                            // 블록 값 초기화
                                            curr[X, Y] = colorIndex;
                                            curr[X - 1, Y] = colorIndex;
                                            curr[X + 1, Y] = colorIndex;
                                            curr[X - 1, Y + 1] = colorIndex;
                                            curr[X - 1, Y - 1] = colorIndex;
                                        }
                                    }

                                    //  ③
                                    //  ㅍ
                                    //①④②
                                    if (
                                       (curr[X - 1, Y - 1] == colorIndex) &&      // 1 왼쪽 아래
                                       (curr[X + 1, Y - 1] == colorIndex) &&   // 2 오른쪽 아래
                                       (curr[X, Y + 1] == colorIndex) &&       // 3 위
                                       (curr[X, Y - 1] == colorIndex)             // 4 아래 
                                       )
                                    {
                                        // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                        curr[X, Y] = num;
                                        curr[X - 1, Y - 1] = num;
                                        curr[X + 1, Y - 1] = num;
                                        curr[X, Y + 1] = num;
                                        curr[X, Y - 1] = num;
                                        // 블록 상태를 사용으로 전환
                                        wbm_List[set].waitBlockCheckList[15] = true;

                                        int nextCount = posListCount + 1;
                                        bool isCheck = false;

                                        // 현재 지정된 색상의 블록 수만큼 반복
                                        // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                        // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                        //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                        for (int i = 0; i < posList.Count; i++)
                                        {
                                            if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                            {
                                                nextCount = i;
                                                isCheck = true;
                                                break;
                                            }
                                        }

                                        // 아직 남은 색상이 존재하면 다시 체크
                                        if (isCheck == false)
                                        {
                                            //Debug.Log("만세 클리어!");
                                            return true;
                                        }

                                        // 남은 부분이 블록으로 커버가 가능하면 리턴
                                        if (ReadingPatternCheck(nextCount))
                                        {
                                            return true;
                                        }
                                        // 커버가 불가능하면 정답 성립 불가능
                                        else
                                        {
                                            // 사용했던 블록 초기화
                                            wbm_List[set].waitBlockCheckList[15] = false;

                                            // 블록 값 초기화
                                            curr[X, Y] = colorIndex;
                                            curr[X - 1, Y - 1] = colorIndex;
                                            curr[X + 1, Y - 1] = colorIndex;
                                            curr[X, Y + 1] = colorIndex;
                                            curr[X, Y - 1] = colorIndex;
                                        }
                                    }

                                    //    ③
                                    //①ㅍ②
                                    //    ④
                                    if (
                                       (curr[X - 1, Y] == colorIndex) &&          // 1 왼쪽
                                       (curr[X + 1, Y] == colorIndex) &&       // 2 오른쪽
                                       (curr[X + 1, Y + 1] == colorIndex) &&   // 3 오른쪽 위
                                       (curr[X + 1, Y - 1] == colorIndex)         // 4 오른쪽 아래
                                       )
                                    {
                                        // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                        curr[X, Y] = num;
                                        curr[X - 1, Y] = num;
                                        curr[X + 1, Y] = num;
                                        curr[X + 1, Y + 1] = num;
                                        curr[X + 1, Y - 1] = num;
                                        // 블록 상태를 사용으로 전환
                                        wbm_List[set].waitBlockCheckList[15] = true;

                                        int nextCount = posListCount + 1;
                                        bool isCheck = false;

                                        // 현재 지정된 색상의 블록 수만큼 반복
                                        // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                        // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                        //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                        for (int i = 0; i < posList.Count; i++)
                                        {
                                            if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                            {
                                                nextCount = i;
                                                isCheck = true;
                                                break;
                                            }
                                        }

                                        // 아직 남은 색상이 존재하면 다시 체크
                                        if (isCheck == false)
                                        {
                                            //Debug.Log("만세 클리어!");
                                            return true;
                                        }

                                        // 남은 부분이 블록으로 커버가 가능하면 리턴
                                        if (ReadingPatternCheck(nextCount))
                                        {
                                            return true;
                                        }
                                        // 커버가 불가능하면 정답 성립 불가능
                                        else
                                        {
                                            // 사용했던 블록 초기화
                                            wbm_List[set].waitBlockCheckList[15] = false;

                                            // 블록 값 초기화
                                            curr[X, Y] = colorIndex;
                                            curr[X - 1, Y] = colorIndex;
                                            curr[X + 1, Y] = colorIndex;
                                            curr[X + 1, Y + 1] = colorIndex;
                                            curr[X + 1, Y - 1] = colorIndex;
                                        }
                                    }
                                }
                                #endregion

                                //E ~~~~~~~~~~~~~~~~~~~~~~~~
                                //①②③
                                //  ④
                                //  ㅍ
                                #region PT1 - 5
                                //①②③
                                //  ④
                                //  ㅍ
                                if (
                                    0 <= X - 1 &&
                                    sizeX > X + 1 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X - 1, Y + 2] == colorIndex) &&      // 왼쪽 위   1
                                   ( curr[X + 1, Y + 2] == colorIndex) &&   // 오른쪽 위 2
                                   ( curr[X, Y + 1] == colorIndex) &&       // 중단 위   3
                                   ( curr[X, Y + 2] == colorIndex)          // 중단 위   4
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y + 2] = num;
                                    curr[X + 1, Y + 2] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }

                                //③
                                //②①ㅍ
                                //④
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 1 &&
                                    sizeY > Y + 1 &&

                                   ( curr[X - 1, Y] == colorIndex) &&          // 왼쪽 1
                                   ( curr[X - 2, Y] == colorIndex) &&          // 왼쪽 2 
                                   ( curr[X - 2, Y + 1] == colorIndex) &&   // 위   3
                                   ( curr[X - 2, Y - 1] == colorIndex)         // 아래 4
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 2, Y + 1] = num;
                                    curr[X - 2, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                        curr[X - 2, Y - 1] = colorIndex;
                                    }
                                }

                                //  ㅍ
                                //  ③
                                //①④②
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 2 &&
                                    sizeX > X + 1 &&

                                   ( curr[X - 1, Y - 2] == colorIndex) &&      // 왼쪽 아래    1
                                   ( curr[X + 1, Y - 2] == colorIndex) &&   // 오른쪽 아래  2
                                   ( curr[X, Y - 1] == colorIndex) &&          // 중단 아래    3
                                   ( curr[X, Y - 2] == colorIndex)             // 중단 아래    4
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y - 2] = num;
                                    curr[X + 1, Y - 2] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }

                                //    ③
                                //ㅍ①②
                                //    ④
                                if (
                                    0 <= Y - 1 &&
                                    sizeX > X + 2 &&
                                    sizeY > Y + 1 &&

                                   ( curr[X + 1, Y] == colorIndex) &&       // 오른쪽      1
                                   ( curr[X + 2, Y] == colorIndex) &&       // 오른쪽      2
                                   ( curr[X + 2, Y + 1] == colorIndex) &&   // 오른쪽 위   3
                                   ( curr[X + 2, Y - 1] == colorIndex)         // 오른쪽 아래 4
                                   )
                                {
                                    // PT1의 모형만큼의 범위를 모두 PT1으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X + 2, Y + 1] = num;
                                    curr[X + 2, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                    }
                                }
                                #endregion
                            }
                        }
                        break;

                    case 12: // PT2
                        //  ㅁ
                        //ㅁㅁㅁ
                        //  ㅁ
                        {
                            if (!Option_PentoMode.isOn) break;

                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;

                            if (wbm_List[set].waitBlockCheckList[12] == false)
                            {
                                // (12000 + 세트순번) 오브젝트 네이밍 세팅
                                int num = 13000 + set;

                                //A ~~~~~~~~~~~~~~~~~~~~~~~~
                                //  ㅍ
                                //①②③
                                //  ④
                                #region PT2 - 1
                                //  ㅍ
                                //①②③
                                //  ④
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 2 &&
                                    sizeX > X + 1 &&

                                   (curr[X - 1, Y - 1] == colorIndex) &&      // 1 왼쪽
                                   ( curr[X + 1, Y - 1] == colorIndex) &&   // 2 오른쪽
                                   ( curr[X, Y - 1] == colorIndex) &&          // 3 중간
                                   ( curr[X, Y - 2] == colorIndex)             // 4 아래
                                   )
                                {
                                    // PT2의 모형만큼의 범위를 모두 PT2으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[12] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[12] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }

                                //  ③
                                //ㅍ①②
                                //  ④
                                if (
                                    0 <= Y - 1 &&
                                    sizeX > X + 2 &&
                                    sizeY > Y + 1 &&

                                   ( curr[X + 1, Y] == colorIndex) &&       // 1 오른쪽
                                   ( curr[X + 2, Y] == colorIndex) &&       // 2 오른쪽
                                   ( curr[X + 1, Y + 1] == colorIndex) &&   // 3 위
                                   ( curr[X + 1, Y - 1] == colorIndex)         // 4 아래
                                   )
                                {
                                    // PT2의 모형만큼의 범위를 모두 PT2으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[12] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[12] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                //  ④
                                //①③②
                                //  ㅍ
                                if (
                                    0 <= X - 1 &&
                                    sizeX < X + 1 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X - 1, Y + 1] == colorIndex) &&      // 1 왼쪽
                                   ( curr[X + 1, Y + 1] == colorIndex) &&   // 2 오른쪽
                                   ( curr[X, Y + 1] == colorIndex) &&       // 3 중간
                                   ( curr[X, Y + 2] == colorIndex)          // 4 위
                                   )
                                {
                                    // PT2의 모형만큼의 범위를 모두 PT2으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[12] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[12] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }

                                //  ③
                                //①②ㅍ
                                //  ④
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 1 &&
                                    sizeY > Y + 1 &&

                                   ( curr[X - 1, Y] == colorIndex) &&          // 1 왼쪽
                                   ( curr[X - 2, Y] == colorIndex) &&          // 2 왼쪽
                                   ( curr[X - 1, Y + 1] == colorIndex) &&   // 3 위  
                                   ( curr[X - 1, Y - 1] == colorIndex)         // 4 아래
                                   )
                                {
                                    // PT2의 모형만큼의 범위를 모두 PT2으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[12] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[12] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                    }
                                }
                                #endregion

                                //B ~~~~~~~~~~~~~~~~~~~~~~~~
                                //  ㅁ
                                //ㅁㅍㅁ
                                //  ㅁ
                                #region PT2 - 2
                                //  ③
                                //①ㅍ②
                                //  ④
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 1 &&
                                    sizeX > X + 1 &&
                                    sizeY > Y + 1 &&

                                   ( curr[X - 1, Y] == colorIndex) &&      // 1 왼쪽  
                                   ( curr[X + 1, Y] == colorIndex) &&   // 2 오른쪽
                                   ( curr[X, Y + 1] == colorIndex) &&   // 3 위    
                                   ( curr[X, Y - 1] == colorIndex)         // 4 아래  
                                   )
                                {
                                    // PT2의 모형만큼의 범위를 모두 PT2으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[12] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[12] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }
                                #endregion
                            }
                        }
                        break;

                    case 13: // PT3
                        //ㅁ
                        //ㅁㅁ
                        //  ㅁㅁ
                        {
                            if (!Option_PentoMode.isOn) break;

                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;

                            if (wbm_List[set].waitBlockCheckList[13] == false)
                            {
                                // (12000 + 세트순번) 오브젝트 네이밍 세팅
                                int num = 14000 + set;
                                //A ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅍ
                                //ㅁㅁ
                                //  ㅁㅁ
                                #region PT3 - 1
                                //ㅍ
                                //①②
                                //  ③④
                                if (
                                    0 <= Y - 2 &&
                                    sizeX > X + 2 &&

                                    (curr[X, Y - 1] == colorIndex) &&       // 1
                                    (curr[X + 1, Y - 1] == colorIndex) &&   // 2
                                    (curr[X + 1, Y - 2] == colorIndex) &&   // 3
                                    (curr[X + 2, Y - 2] == colorIndex)      // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 1, Y - 2] = num;
                                    curr[X + 2, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                        curr[X + 2, Y - 2] = colorIndex;
                                    }
                                }

                                //    ④
                                //  ②③
                                //ㅍ①
                                if (
                                    sizeX > X + 2 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X + 1, Y] == colorIndex) &&       // 1
                                   ( curr[X + 1, Y + 1] == colorIndex) &&   // 2
                                   ( curr[X + 2, Y + 1] == colorIndex) &&   // 3
                                   ( curr[X + 2, Y + 2] == colorIndex)      // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 2, Y + 1] = num;
                                    curr[X + 2, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                        curr[X + 2, Y + 2] = colorIndex;
                                    }
                                }

                                //④③
                                //  ②①
                                //    ㅍ
                                if (
                                    0 <= X - 2 &&
                                    sizeY > Y + 2 &&

                                   (curr[X, Y + 1] == colorIndex) &&       // 1
                                   (curr[X - 1, Y + 1] == colorIndex) &&   // 2
                                   (curr[X - 1, Y + 2] == colorIndex) &&   // 3
                                   (curr[X - 2, Y + 2] == colorIndex)      // 4
                                   )
                                {

                                    Debug.Log("Curr PT3 - R2 : " + colorIndex
                                        + "\n" + curr[X, Y]
                                        + "\n" + curr[X, Y + 1]
                                        + "\n" + curr[X - 1, Y + 1]
                                        + "\n" + curr[X - 1, Y + 2]
                                        + "\n" + curr[X - 2, Y + 2]
                                        );

                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y + 2] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 2, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 2, Y + 2] = colorIndex;
                                    }
                                }

                                //  ①ㅍ
                                //③②
                                //④
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 2 &&

                                   ( curr[X - 1, Y] == colorIndex) &&      // 1
                                   ( curr[X - 1, Y - 1] == colorIndex) &&  // 2
                                   ( curr[X - 2, Y - 1] == colorIndex) &&  // 3
                                   ( curr[X - 2, Y - 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 2, Y - 1] = num;
                                    curr[X - 2, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 2, Y - 1] = colorIndex;
                                        curr[X - 2, Y - 2] = colorIndex;
                                    }
                                }

                                #endregion

                                //B ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁ
                                //ㅍㅁ
                                //  ㅁㅁ
                                #region PT3 - 2
                                //①
                                //ㅍ②
                                //  ③④
                                if (
                                    0 <= Y - 1 &&
                                    sizeX > X + 2 &&
                                    sizeY > Y + 1 &&

                                   ( curr[X, Y + 1] == colorIndex) &&   // 1
                                   ( curr[X + 1, Y] == colorIndex) &&   // 2
                                   ( curr[X + 1, Y - 1] == colorIndex) &&  // 3
                                   ( curr[X + 2, Y - 1] == colorIndex)  // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 2, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                    }
                                }

                                //    ④
                                //  ②③
                                //①ㅍ
                                if (
                                    0 <= X - 1 &&
                                    sizeX > X + 1 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X - 1, Y] == colorIndex) &&          // 1
                                   ( curr[X, Y + 1] == colorIndex) &&       // 2
                                   ( curr[X + 1, Y + 1] == colorIndex) &&   // 3
                                   ( curr[X + 1, Y + 2] == colorIndex)      // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 1, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                    }
                                }

                                //④③
                                //  ②ㅍ
                                //    ①
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 1 &&
                                    sizeY > Y + 1 &&

                                   ( curr[X, Y - 1] == colorIndex) &&      // 1
                                   ( curr[X - 1, Y] == colorIndex) &&      // 2
                                   ( curr[X - 1, Y + 1] == colorIndex) &&  // 3
                                   ( curr[X - 2, Y + 1] == colorIndex)     // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 2, Y + 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                    }
                                }

                                //  ㅍ①
                                //③②
                                //④
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 2 &&
                                    sizeX > X + 1 &&

                                   ( curr[X + 1, Y] == colorIndex) &&   // 1
                                   ( curr[X, Y - 1] == colorIndex) &&      // 2
                                   ( curr[X - 1, Y - 1] == colorIndex) &&  // 3
                                   ( curr[X - 1, Y - 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 1, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                    }
                                }

                                #endregion

                                //C ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁ
                                //ㅁㅍ
                                //  ㅁㅁ
                                #region PT3 - 3
                                //①
                                //②ㅍ
                                //  ③④
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 1 &&
                                    sizeX > X + 1 &&
                                    sizeY > Y + 1
                                    )
                                {

                                    if (
                                       (curr[X - 1, Y + 1] == colorIndex) &&   // 1
                                       (curr[X - 1, Y] == colorIndex) &&          // 2
                                       (curr[X, Y - 1] == colorIndex) &&          // 3
                                       (curr[X + 1, Y - 1] == colorIndex)      // 4
                                       )
                                    {
                                        // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                        curr[X, Y] = num;
                                        curr[X - 1, Y + 1] = num;
                                        curr[X - 1, Y] = num;
                                        curr[X, Y - 1] = num;
                                        curr[X + 1, Y - 1] = num;
                                        // 블록 상태를 사용으로 전환
                                        wbm_List[set].waitBlockCheckList[13] = true;

                                        int nextCount = posListCount + 1;
                                        bool isCheck = false;

                                        // 현재 지정된 색상의 블록 수만큼 반복
                                        // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                        // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                        //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                        for (int i = 0; i < posList.Count; i++)
                                        {
                                            if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                            {
                                                nextCount = i;
                                                isCheck = true;
                                                break;
                                            }
                                        }

                                        // 아직 남은 색상이 존재하면 다시 체크
                                        if (isCheck == false)
                                        {
                                            //Debug.Log("만세 클리어!");
                                            return true;
                                        }

                                        // 남은 부분이 블록으로 커버가 가능하면 리턴
                                        if (ReadingPatternCheck(nextCount))
                                        {
                                            return true;
                                        }
                                        // 커버가 불가능하면 정답 성립 불가능
                                        else
                                        {
                                            // 사용했던 블록 초기화
                                            wbm_List[set].waitBlockCheckList[13] = false;

                                            // 블록 값 초기화
                                            curr[X, Y] = colorIndex;
                                            curr[X - 1, Y + 1] = colorIndex;
                                            curr[X - 1, Y] = colorIndex;
                                            curr[X, Y - 1] = colorIndex;
                                            curr[X + 1, Y - 1] = colorIndex;
                                        }
                                    }

                                    //    ①
                                    //  ㅍ②
                                    //④③
                                    if (
                                       (curr[X + 1, Y + 1] == colorIndex) &&   // 1
                                       (curr[X + 1, Y] == colorIndex) &&       // 2
                                       (curr[X, Y - 1] == colorIndex) &&          // 3
                                       (curr[X - 1, Y - 1] == colorIndex)      // 4
                                       )
                                    {
                                        // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                        curr[X, Y] = num;
                                        curr[X + 1, Y + 1] = num;
                                        curr[X + 1, Y] = num;
                                        curr[X, Y - 1] = num;
                                        curr[X - 1, Y - 1] = num;
                                        // 블록 상태를 사용으로 전환
                                        wbm_List[set].waitBlockCheckList[13] = true;

                                        int nextCount = posListCount + 1;
                                        bool isCheck = false;

                                        // 현재 지정된 색상의 블록 수만큼 반복
                                        // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                        // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                        //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                        for (int i = 0; i < posList.Count; i++)
                                        {
                                            if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                            {
                                                nextCount = i;
                                                isCheck = true;
                                                break;
                                            }
                                        }

                                        // 아직 남은 색상이 존재하면 다시 체크
                                        if (isCheck == false)
                                        {
                                            //Debug.Log("만세 클리어!");
                                            return true;
                                        }

                                        // 남은 부분이 블록으로 커버가 가능하면 리턴
                                        if (ReadingPatternCheck(nextCount))
                                        {
                                            return true;
                                        }
                                        // 커버가 불가능하면 정답 성립 불가능
                                        else
                                        {
                                            // 사용했던 블록 초기화
                                            wbm_List[set].waitBlockCheckList[13] = false;

                                            // 블록 값 초기화
                                            curr[X, Y] = colorIndex;
                                            curr[X + 1, Y + 1] = colorIndex;
                                            curr[X + 1, Y] = colorIndex;
                                            curr[X, Y - 1] = colorIndex;
                                            curr[X - 1, Y - 1] = colorIndex;
                                        }
                                    }

                                    //①②
                                    //  ㅍ③
                                    //    ④
                                    if (
                                       (curr[X - 1, Y + 1] == colorIndex) &&  // 1
                                       (curr[X, Y + 1] == colorIndex) &&   // 2
                                       (curr[X + 1, Y] == colorIndex) &&   // 3
                                       (curr[X + 1, Y - 1] == colorIndex)     // 4
                                       )
                                    {
                                        // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                        curr[X, Y] = num;
                                        curr[X - 1, Y + 1] = num;
                                        curr[X, Y + 1] = num;
                                        curr[X + 1, Y] = num;
                                        curr[X + 1, Y - 1] = num;
                                        // 블록 상태를 사용으로 전환
                                        wbm_List[set].waitBlockCheckList[13] = true;

                                        int nextCount = posListCount + 1;
                                        bool isCheck = false;

                                        // 현재 지정된 색상의 블록 수만큼 반복
                                        // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                        // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                        //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                        for (int i = 0; i < posList.Count; i++)
                                        {
                                            if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                            {
                                                nextCount = i;
                                                isCheck = true;
                                                break;
                                            }
                                        }

                                        // 아직 남은 색상이 존재하면 다시 체크
                                        if (isCheck == false)
                                        {
                                            //Debug.Log("만세 클리어!");
                                            return true;
                                        }

                                        // 남은 부분이 블록으로 커버가 가능하면 리턴
                                        if (ReadingPatternCheck(nextCount))
                                        {
                                            return true;
                                        }
                                        // 커버가 불가능하면 정답 성립 불가능
                                        else
                                        {
                                            // 사용했던 블록 초기화
                                            wbm_List[set].waitBlockCheckList[13] = false;

                                            // 블록 값 초기화
                                            curr[X, Y] = colorIndex;
                                            curr[X - 1, Y + 1] = colorIndex;
                                            curr[X, Y + 1] = colorIndex;
                                            curr[X + 1, Y] = colorIndex;
                                            curr[X + 1, Y - 1] = colorIndex;
                                        }
                                    }

                                    //  ①②
                                    //③ㅍ
                                    //④
                                    if (
                                       (curr[X, Y + 1] == colorIndex) &&       // 1
                                       (curr[X + 1, Y + 1] == colorIndex) &&   // 2
                                       (curr[X - 1, Y] == colorIndex) &&          // 3
                                       (curr[X - 1, Y - 1] == colorIndex)         // 4
                                       )
                                    {
                                        // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                        curr[X, Y] = num;
                                        curr[X, Y + 1] = num;
                                        curr[X + 1, Y + 1] = num;
                                        curr[X - 1, Y] = num;
                                        curr[X - 1, Y - 1] = num;
                                        // 블록 상태를 사용으로 전환
                                        wbm_List[set].waitBlockCheckList[13] = true;

                                        int nextCount = posListCount + 1;
                                        bool isCheck = false;

                                        // 현재 지정된 색상의 블록 수만큼 반복
                                        // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                        // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                        //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                        for (int i = 0; i < posList.Count; i++)
                                        {
                                            if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                            {
                                                nextCount = i;
                                                isCheck = true;
                                                break;
                                            }
                                        }

                                        // 아직 남은 색상이 존재하면 다시 체크
                                        if (isCheck == false)
                                        {
                                            //Debug.Log("만세 클리어!");
                                            return true;
                                        }

                                        // 남은 부분이 블록으로 커버가 가능하면 리턴
                                        if (ReadingPatternCheck(nextCount))
                                        {
                                            return true;
                                        }
                                        // 커버가 불가능하면 정답 성립 불가능
                                        else
                                        {
                                            // 사용했던 블록 초기화
                                            wbm_List[set].waitBlockCheckList[13] = false;

                                            // 블록 값 초기화
                                            curr[X, Y] = colorIndex;
                                            curr[X, Y + 1] = colorIndex;
                                            curr[X + 1, Y + 1] = colorIndex;
                                            curr[X - 1, Y] = colorIndex;
                                            curr[X - 1, Y - 1] = colorIndex;
                                        }
                                    }
                                }

                                #endregion

                                //D ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁ
                                //ㅁㅁ
                                //  ㅍㅁ
                                #region PT3 - 4
                                //①
                                //②③
                                //  ㅍ④
                                if (
                                    0 <= X - 1 &&
                                    sizeX > X + 1 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X - 1, Y + 2] == colorIndex) &&   // 1
                                   ( curr[X - 1, Y + 1] == colorIndex) &&      // 2
                                   ( curr[X, Y + 1] == colorIndex) &&       // 3
                                   ( curr[X + 1, Y] == colorIndex)          // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y + 2] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                    }
                                }

                                //    ①
                                //  ②ㅍ
                                //④③
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 1 &&
                                    sizeY > Y + 1 &&

                                   ( curr[X, Y + 1] == colorIndex) &&   // 1
                                   ( curr[X - 1, Y] == colorIndex) &&      // 2
                                   ( curr[X - 1, Y - 1] == colorIndex) &&  // 3
                                   ( curr[X - 2, Y - 1] == colorIndex)     // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 2, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 2, Y - 1] = colorIndex;
                                    }
                                }

                                //①ㅍ
                                //  ②③
                                //    ④
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 2 &&
                                    sizeX > X + 1 &&

                                   ( curr[X - 1, Y] == colorIndex) &&          // 1
                                   ( curr[X, Y - 1] == colorIndex) &&          // 2
                                   ( curr[X + 1, Y - 1] == colorIndex) &&   // 3
                                   ( curr[X + 1, Y - 2] == colorIndex)         // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 1, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                    }
                                }

                                //  ③④
                                //ㅍ②
                                //①
                                if (
                                    0 <= Y - 1 &&
                                    sizeX > X + 2 &&
                                    sizeY > Y + 1 &&

                                   ( curr[X, Y - 1] == colorIndex) &&          // 1
                                   ( curr[X + 1, Y] == colorIndex) &&       // 2
                                   ( curr[X + 1, Y + 1] == colorIndex) &&   // 3
                                   ( curr[X + 2, Y + 1] == colorIndex)      // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 2, Y + 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                    }
                                }
                                #endregion

                                //E ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁ
                                //ㅁㅁ
                                //  ㅁㅍ
                                #region PT3 - 5
                                //④
                                //③②
                                //  ①ㅍ
                                if (
                                    0 <= X - 2 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X - 1, Y] == colorIndex) &&       // 1
                                   ( curr[X - 1, Y + 1] == colorIndex) &&   // 2
                                   ( curr[X - 2, Y + 1] == colorIndex) &&   // 3
                                   ( curr[X - 2, Y + 2] == colorIndex)      // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 2, Y + 1] = num;
                                    curr[X - 2, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                        curr[X - 2, Y + 2] = colorIndex;
                                    }
                                }

                                //    ㅍ
                                //  ②①
                                //④③
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 2 &&

                                   ( curr[X, Y - 1] == colorIndex) &&      // 1
                                   ( curr[X - 1, Y - 1] == colorIndex) &&  // 2
                                   ( curr[X - 1, Y - 2] == colorIndex) &&  // 3
                                   ( curr[X - 2, Y - 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 1, Y - 2] = num;
                                    curr[X - 2, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                        curr[X - 2, Y - 2] = colorIndex;
                                    }
                                }

                                //ㅍ①
                                //  ②③
                                //    ④
                                if (
                                    0 <= Y - 2 &&
                                    sizeX > X + 2 &&

                                   ( curr[X + 1, Y] == colorIndex) &&      // 1
                                   ( curr[X + 1, Y - 1] == colorIndex) &&  // 2
                                   ( curr[X + 2, Y - 1] == colorIndex) &&  // 3
                                   ( curr[X + 2, Y - 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 2, Y - 1] = num;
                                    curr[X + 2, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                        curr[X + 2, Y - 2] = colorIndex;
                                    }
                                }

                                //  ③④
                                //①②
                                //ㅍ
                                if (
                                    sizeX > X + 2 &&
                                    sizeY > Y + 2 &&

                                   ( curr[X, Y + 1] == colorIndex) &&      // 1
                                   ( curr[X + 1, Y + 1] == colorIndex) &&  // 2
                                   ( curr[X + 1, Y + 2] == colorIndex) &&  // 3
                                   ( curr[X + 2, Y + 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT3의 모형만큼의 범위를 모두 PT3으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 1, Y + 2] = num;
                                    curr[X + 2, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[13] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[13] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                        curr[X + 2, Y + 2] = colorIndex;
                                    }
                                }

                                #endregion
                            }
                        }
                        break;

                    case 14: // PT4
                        //ㅁㅁㅁ
                        //ㅁ
                        //ㅁ
                        {
                            if (!Option_PentoMode.isOn) break;

                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;

                            if (wbm_List[set].waitBlockCheckList[14] == false)
                            {
                                // (12000 + 세트순번) 오브젝트 네이밍 세팅
                                int num = 15000 + set;
                                //A ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅍㅁㅁ
                                //ㅁ
                                //ㅁ
                                #region PT4 - 1
                                //ㅍ①②
                                //③
                                //④
                                if (
                                    0 <= Y - 2 &&
                                    sizeX > X + 2 &&

                                   (curr[X + 1, Y] == colorIndex) &&   // 1
                                   (curr[X + 2, Y] == colorIndex) &&   // 2
                                   (curr[X, Y - 1] == colorIndex) &&      // 3
                                   (curr[X, Y - 2] == colorIndex)         // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }

                                //④
                                //③
                                //ㅍ①②
                                if (
                                    sizeX > X + 2 &&
                                    sizeY > Y + 2 &&

                                   (curr[X + 1, Y] == colorIndex) &&   // 1
                                   (curr[X + 2, Y] == colorIndex) &&   // 2
                                   (curr[X, Y + 1] == colorIndex) &&   // 3
                                   (curr[X, Y + 2] == colorIndex)      // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }

                                //    ④
                                //    ③
                                //①②ㅍ
                                if (
                                    0 <= X - 2 &&
                                    sizeY > Y + 2 &&

                                    (curr[X - 2, Y] == colorIndex) &&     // 1
                                    (curr[X - 1, Y] == colorIndex) &&     // 2
                                    (curr[X, Y + 1] == colorIndex) &&  // 3
                                    (curr[X, Y + 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }

                                //①②ㅍ
                                //    ③
                                //    ④
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 2 &&

                                    (curr[X - 2, Y] == colorIndex) &&     // 1
                                    (curr[X - 1, Y] == colorIndex) &&     // 2
                                    (curr[X, Y - 1] == colorIndex) &&     // 3
                                    (curr[X, Y - 2] == colorIndex)        // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }

                                #endregion

                                //B ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁㅍㅁ
                                //ㅁ
                                //ㅁ
                                #region PT4 - 2
                                //①ㅍ②
                                //③
                                //④
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 2 &&
                                    sizeX > X + 1 &&

                                    (curr[X - 1, Y] == colorIndex) &&     // 1
                                    (curr[X + 1, Y] == colorIndex) &&  // 2
                                    (curr[X - 1, Y - 1] == colorIndex) && // 3
                                    (curr[X - 1, Y - 2] == colorIndex)    // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 1, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                    }
                                }

                                //①
                                //ㅍ
                                //②③④
                                if (
                                    0 <= Y - 1 &&
                                    sizeX > X + 2 &&
                                    sizeY > Y + 1 &&

                                    ( curr[X, Y + 1] == colorIndex) &&      // 1
                                    ( curr[X, Y - 1] == colorIndex) &&         // 2
                                    ( curr[X + 1, Y - 1] == colorIndex) &&  // 3
                                    ( curr[X + 2, Y - 1] == colorIndex)     // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 2, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                    }
                                }

                                //    ④
                                //    ③
                                //①ㅍ②
                                if (
                                    0 <= X - 1 &&
                                    sizeX > X + 1 &&
                                    sizeY > Y + 2 &&

                                    ( curr[X - 1, Y] == colorIndex) &&         // 1
                                    ( curr[X + 1, Y] == colorIndex) &&      // 2
                                    ( curr[X + 1, Y + 1] == colorIndex) &&  // 3
                                    ( curr[X + 1, Y + 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 1, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                    }
                                }

                                //①②③
                                //    ㅍ
                                //    ④
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 1 &&
                                    sizeY > Y + 1 &&

                                    ( curr[X - 2, Y + 1] == colorIndex) && // 1
                                    ( curr[X - 1, Y + 1] == colorIndex) && // 2
                                    ( curr[X, Y + 1] == colorIndex) &&  // 3
                                    ( curr[X, Y - 1] == colorIndex)        // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 2, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }

                                #endregion

                                //C ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁㅁㅍ
                                //ㅁ
                                //ㅁ
                                #region PT4 - 3
                                //①②ㅍ
                                //③
                                //④
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 2 &&

                                    (curr[X - 2, Y] == colorIndex) &&     // 1
                                    (curr[X - 1, Y] == colorIndex) &&     // 2
                                    (curr[X - 2, Y - 1] == colorIndex) && // 3
                                    (curr[X - 2, Y - 2] == colorIndex)    // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y - 1] = num;
                                    curr[X - 2, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y - 1] = colorIndex;
                                        curr[X - 2, Y - 2] = colorIndex;
                                    }
                                }

                                //ㅍ
                                //①
                                //②③④
                                if (
                                    0 <= Y - 2 &&
                                    sizeX > X + 2 &&

                                    (curr[X, Y - 1] == colorIndex) &&         // 1
                                    (curr[X, Y - 2] == colorIndex) &&         // 2
                                    (curr[X + 1, Y - 2] == colorIndex) &&  // 3
                                    (curr[X + 2, Y - 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    curr[X + 1, Y - 2] = num;
                                    curr[X + 2, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                        curr[X + 2, Y - 2] = colorIndex;
                                    }
                                }

                                //    ④
                                //    ③
                                //ㅍ①②
                                if (
                                    sizeX > X + 2 &&
                                    sizeY > Y + 2 &&

                                    (curr[X + 1, Y] == colorIndex) &&      // 1
                                    (curr[X + 2, Y] == colorIndex) &&      // 2
                                    (curr[X + 2, Y + 1] == colorIndex) &&  // 3
                                    (curr[X + 2, Y + 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X + 2, Y + 1] = num;
                                    curr[X + 2, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                        curr[X + 2, Y + 2] = colorIndex;
                                    }
                                }

                                //④③②
                                //    ①
                                //    ㅍ
                                if (
                                    0 <= X - 2 &&
                                    Y + 2 < sizeY &&

                                    curr[X, Y + 1] == colorIndex &&  // 1
                                    curr[X, Y + 2] == colorIndex && // 2
                                    curr[X - 1, Y + 2] == colorIndex && // 3
                                    curr[X - 2, Y + 2] == colorIndex    // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 2, Y + 2] = num;
                                    curr[X - 1, Y + 2] = num;
                                    curr[X, Y + 2] = num;
                                    curr[X, Y + 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 2, Y + 2] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                    }
                                }

                                #endregion

                                //D ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁㅁㅁ
                                //ㅍ
                                //ㅁ
                                #region PT4 - 4
                                //①②③
                                //ㅍ
                                //④
                                if (
                                    0 <= Y - 1 &&
                                    sizeX > X + 2 &&
                                    sizeY > Y + 1 &&

                                    curr[X, Y + 1] == colorIndex &&         // 1
                                    curr[X + 1, Y + 1] == colorIndex &&     // 2
                                    curr[X + 2, Y + 1] == colorIndex &&     // 3
                                    curr[X, Y - 1] == colorIndex            // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 2, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                    }
                                }

                                //④
                                //③
                                //②ㅍ①
                                if (
                                    0 <= X - 1 &&
                                    sizeX > X + 1 &&
                                    sizeY > Y + 2 &&

                                    (curr[X + 1, Y] == colorIndex) &&      // 1
                                    (curr[X - 1, Y] == colorIndex) &&         // 2
                                    (curr[X - 1, Y + 1] == colorIndex) &&  // 3
                                    (curr[X - 1, Y + 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 1, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                    }
                                }

                                //    ①
                                //    ㅍ
                                //②③④
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 1 &&
                                    sizeY > Y + 1 &&

                                    (curr[X, Y + 1] == colorIndex) &&  // 1
                                    (curr[X - 2, Y - 1] == colorIndex) && // 2
                                    (curr[X - 1, Y - 1] == colorIndex) && // 3
                                    (curr[X, Y - 1] == colorIndex)        // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 1, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                    }
                                }

                                //①ㅍ②
                                //    ③
                                //    ④
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 2 &&
                                    sizeX > X + 1 &&

                                    (curr[X - 1, Y] == colorIndex) &&     // 1
                                    (curr[X + 1, Y] == colorIndex) &&  // 2
                                    (curr[X + 1, Y - 1] == colorIndex) && // 3
                                    (curr[X + 1, Y - 2] == colorIndex)    // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 1, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                    }
                                }

                                #endregion

                                //E ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁㅁㅁ
                                //ㅁ
                                //ㅍ
                                #region PT4 - 5
                                //②③④
                                //①
                                //ㅍ
                                if (
                                    sizeX > X + 2 &&
                                    sizeY > Y + 2 &&

                                    ( curr[X, Y + 1] == colorIndex) &&      // 1
                                    ( curr[X, Y + 2] == colorIndex) &&      // 2
                                    ( curr[X + 1, Y + 2] == colorIndex) &&  // 3
                                    ( curr[X + 2, Y + 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    curr[X + 1, Y + 2] = num;
                                    curr[X + 2, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                        curr[X + 2, Y + 2] = colorIndex;
                                    }
                                }

                                //④
                                //③
                                //②①ㅍ
                                if (
                                    0 <= X - 2 &&
                                    sizeY > Y + 2 &&

                                    ( curr[X - 1, Y] == colorIndex) &&         // 1
                                    ( curr[X - 2, Y] == colorIndex) &&         // 2
                                    ( curr[X - 2, Y + 1] == colorIndex) &&  // 3
                                    ( curr[X - 2, Y + 2] == colorIndex)     // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 2, Y + 1] = num;
                                    curr[X - 2, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                        curr[X - 2, Y + 2] = colorIndex;
                                    }
                                }

                                //    ㅍ
                                //    ①
                                //④③②
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 2 &&

                                    ( curr[X, Y - 1] == colorIndex) &&     // 1
                                    ( curr[X, Y - 2] == colorIndex) &&     // 2
                                    ( curr[X - 1, Y - 2] == colorIndex) && // 3
                                    ( curr[X - 2, Y - 2] == colorIndex)    // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    curr[X - 1, Y - 2] = num;
                                    curr[X - 2, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                        curr[X - 2, Y - 2] = colorIndex;
                                    }
                                }

                                //ㅍ①②
                                //    ③
                                //    ④
                                if (
                                    0 <= Y - 2 &&
                                    sizeX > X + 2 &&

                                    (curr[X + 1, Y] == colorIndex) &&  // 1
                                    ( curr[X + 2, Y] == colorIndex) &&  // 2
                                    ( curr[X + 2, Y - 1] == colorIndex) && // 3
                                    ( curr[X + 2, Y - 2] == colorIndex)    // 4
                                   )
                                {
                                    // PT4의 모형만큼의 범위를 모두 PT4으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X + 2, Y - 1] = num;
                                    curr[X + 2, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[14] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[14] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                        curr[X + 2, Y - 2] = colorIndex;
                                    }
                                }

                                #endregion
                            }
                        }
                        break;

                    case 15: // PT5
                        //ㅁㅁ
                        //ㅁ
                        //ㅁㅁ
                        {
                            if (!Option_PentoMode.isOn) break;

                            if (((isCheckPixel_4 == false && isCheckPixel_3 == false) || (isCheckPixel_4 && isCheckPixel_3)) == false) break;

                            if (wbm_List[set].waitBlockCheckList[15] == false)
                            {
                                // (12000 + 세트순번) 오브젝트 네이밍 세팅
                                int num = 16000 + set;
                                //A ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁㅍ
                                //ㅁ
                                //ㅁㅁ
                                #region PT5 - 1
                                //①ㅍ
                                //②
                                //③④
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 2 &&

                                    ( curr[X - 1, Y] == colorIndex) &&     // 1
                                    ( curr[X - 1, Y - 1] == colorIndex) && // 2
                                    ( curr[X - 1, Y - 2] == colorIndex) && // 3
                                    curr[X, Y - 2] == colorIndex                        // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 1, Y - 2] = num;
                                    curr[X, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }

                                //ㅍ  ④
                                //①②③
                                if (
                                    0 <= Y - 1 &&
                                    sizeX > X + 2 &&

                                    curr[X, Y - 1] == colorIndex &&         // 1
                                    curr[X + 1, Y - 1] == colorIndex &&  // 2
                                    curr[X + 2, Y - 1] == colorIndex &&  // 3
                                    curr[X + 2, Y] == colorIndex                            // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 2, Y - 1] = num;
                                    curr[X + 2, Y] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                    }
                                }

                                //④③
                                //  ②
                                //ㅍ①
                                if (
                                    sizeX > X + 1 &&
                                    sizeY > Y + 2 &&

                                    ( curr[X + 1, Y] == colorIndex) &&      // 1
                                    ( curr[X + 1, Y + 1] == colorIndex) &&  // 2
                                    ( curr[X + 1, Y + 2] == colorIndex) &&  // 3
                                    curr[X, Y + 2] == colorIndex                            // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 1, Y + 2] = num;
                                    curr[X, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }

                                //③②①
                                //④  ㅍ
                                if (
                                    0 <= X - 2 &&
                                    sizeY > Y + 1 &&

                                    ( curr[X, Y + 1] == colorIndex) &&  // 1
                                    ( curr[X - 1, Y + 1] == colorIndex) && // 2
                                    ( curr[X - 2, Y + 1] == colorIndex) && // 3
                                    curr[X - 2, Y] == colorIndex                        // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 2, Y + 1] = num;
                                    curr[X - 2, Y] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                    }
                                }

                                #endregion

                                //B ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅍㅁ
                                //ㅁ
                                //ㅁㅁ
                                #region PT5 - 2
                                //ㅍ①
                                //②
                                //③④
                                if (
                                    0 <= Y - 2 &&
                                    sizeX > X + 1 &&

                                    (curr[X + 1, Y] == colorIndex) &&  // 1
                                    ( curr[X, Y - 1] == colorIndex) &&     // 2
                                    ( curr[X, Y - 2] == colorIndex) &&     // 3
                                    curr[X + 1, Y - 2] == colorIndex                    // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    curr[X + 1, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                    }
                                }

                                //①  ④
                                //ㅍ②③
                                if (
                                    sizeX > X + 2 &&
                                    sizeY > Y + 1 &&

                                    ( curr[X, Y + 1] == colorIndex) &&  // 1
                                    ( curr[X + 1, Y] == colorIndex) &&  // 2
                                    ( curr[X + 2, Y] == colorIndex) &&  // 3
                                    curr[X + 2, Y + 1] == colorIndex                    // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X + 2, Y + 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                    }
                                }

                                //④③
                                //  ②
                                //①ㅍ
                                if (
                                    0 <= X - 1 &&
                                    sizeY > Y + 2 &&

                                    ( curr[X - 1, Y] == colorIndex) &&     // 1
                                    ( curr[X, Y + 1] == colorIndex) &&  // 2
                                    ( curr[X, Y + 2] == colorIndex) &&  // 3
                                    curr[X - 1, Y + 2] == colorIndex                    // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    curr[X - 1, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                    }
                                }

                                //③②ㅍ
                                //④  ①
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 1 &&

                                    ( curr[X, Y - 1] == colorIndex) && // 1
                                    ( curr[X - 1, Y] == colorIndex) && // 2
                                    ( curr[X - 2, Y] == colorIndex) && // 3
                                    curr[X - 2, Y - 1] == colorIndex                // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X - 2, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X - 2, Y - 1] = colorIndex;
                                    }
                                }

                                #endregion

                                //C ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁㅁ
                                //ㅍ
                                //ㅁㅁ
                                #region PT5 - 3
                                //①②
                                //ㅍ
                                //③④
                                if (
                                    0 <= Y - 1 &&
                                    sizeX > X + 1 &&
                                    sizeY > Y + 1 &&

                                    ( curr[X, Y + 1] == colorIndex) &&  // 1
                                    ( curr[X + 1, Y + 1] == colorIndex) && // 2
                                    ( curr[X, Y - 1] == colorIndex) &&     // 3
                                    curr[X + 1, Y - 1] == colorIndex                    // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                //②  ④
                                //①ㅍ③
                                if (
                                    0 <= X - 1 &&
                                    sizeX > X + 1 &&
                                    sizeY > Y + 1 &&

                                    ( curr[X - 1, Y] == colorIndex) &&         // 1
                                    ( curr[X - 1, Y + 1] == colorIndex) &&  // 2
                                    ( curr[X + 1, Y] == colorIndex) &&      // 3
                                    curr[X + 1, Y + 1] == colorIndex                        // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y + 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                    }
                                }

                                //④③
                                //  ㅍ
                                //②①
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 1 &&
                                    sizeY > Y + 1 &&

                                    ( curr[X, Y - 1] == colorIndex) &&     // 1
                                    ( curr[X - 1, Y - 1] == colorIndex) && // 2
                                    ( curr[X, Y + 1] == colorIndex) &&  // 3
                                    curr[X - 1, Y + 1] == colorIndex                    // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 1, Y + 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                    }
                                }

                                //②ㅍ①
                                //③  ④
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 1 &&
                                    sizeX > X + 1 &&

                                    ( curr[X + 1, Y] == colorIndex) &&  // 1
                                    ( curr[X - 1, Y] == colorIndex) &&     // 2
                                    ( curr[X - 1, Y - 1] == colorIndex) && // 3
                                    curr[X + 1, Y - 1] == colorIndex                    // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X + 1, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                    }
                                }

                                #endregion

                                //D ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁㅁ
                                //ㅁ
                                //ㅍㅁ
                                #region PT5 - 4
                                //③④
                                //②
                                //ㅍ①
                                if (
                                    sizeX > X + 1 &&
                                    sizeY > Y + 2 &&

                                    ( curr[X + 1, Y] == colorIndex) &&  // 1
                                    ( curr[X, Y + 1] == colorIndex) &&  // 2
                                    ( curr[X, Y + 2] == colorIndex) &&  // 3
                                    curr[X + 1, Y + 2] == colorIndex                    // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X, Y + 2] = num;
                                    curr[X + 1, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                        curr[X + 1, Y + 2] = colorIndex;
                                    }
                                }

                                //④  ③
                                //②①ㅍ
                                if (
                                    0 <= X - 2 &&
                                    sizeY > Y + 1 &&

                                    ( curr[X - 1, Y] == colorIndex) &&     // 1
                                    ( curr[X - 2, Y] == colorIndex) &&     // 2
                                    ( curr[X, Y + 1] == colorIndex) &&  // 3
                                    curr[X - 2, Y + 1] == colorIndex                    // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 2, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X - 2, Y + 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X - 2, Y + 1] = colorIndex;
                                    }
                                }

                                //①ㅍ
                                //  ②
                                //④③
                                if (
                                    0 <= X - 1 &&
                                    0 <= Y - 2 &&

                                    ( curr[X - 1, Y] == colorIndex) && // 1
                                    ( curr[X, Y - 1] == colorIndex) && // 2
                                    ( curr[X, Y - 2] == colorIndex) && // 3
                                    curr[X - 1, Y - 2] == colorIndex                // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X, Y - 2] = num;
                                    curr[X - 1, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                        curr[X - 1, Y - 2] = colorIndex;
                                    }
                                }

                                //ㅍ①②
                                //③  ④
                                if (
                                    0 <= Y - 1 &&
                                    sizeX > X + 2 &&

                                    (curr[X + 1, Y] == colorIndex) &&  // 1
                                    ( curr[X + 2, Y] == colorIndex) &&  // 2
                                    ( curr[X, Y - 1] == colorIndex) &&     // 3
                                    curr[X + 2, Y - 1] == colorIndex                    // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 2, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X + 2, Y - 1] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X + 2, Y - 1] = colorIndex;
                                    }
                                }

                                #endregion

                                //E ~~~~~~~~~~~~~~~~~~~~~~~~
                                //ㅁㅁ
                                //ㅁ
                                //ㅁㅍ
                                #region PT5 - 5
                                //③④
                                //②
                                //①ㅍ
                                if (
                                    0 <= X - 1 &&
                                    sizeY > Y + 2 &&

                                    ( curr[X - 1, Y] == colorIndex) &&         // 1
                                    ( curr[X - 1, Y + 1] == colorIndex) &&  // 2
                                    ( curr[X - 1, Y + 2] == colorIndex) &&  // 3
                                    curr[X, Y + 2] == colorIndex                            // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X - 1, Y] = num;
                                    curr[X - 1, Y + 1] = num;
                                    curr[X - 1, Y + 2] = num;
                                    curr[X, Y + 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X - 1, Y] = colorIndex;
                                        curr[X - 1, Y + 1] = colorIndex;
                                        curr[X - 1, Y + 2] = colorIndex;
                                        curr[X, Y + 2] = colorIndex;
                                    }
                                }

                                //④  ㅍ
                                //③②①
                                if (
                                    0 <= X - 2 &&
                                    0 <= Y - 1 &&

                                    ( curr[X, Y - 1] == colorIndex) &&     // 1
                                    ( curr[X - 1, Y - 1] == colorIndex) && // 2
                                    ( curr[X - 2, Y - 1] == colorIndex) && // 3
                                    curr[X - 2, Y] == colorIndex                        // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y - 1] = num;
                                    curr[X - 1, Y - 1] = num;
                                    curr[X - 2, Y - 1] = num;
                                    curr[X - 2, Y] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y - 1] = colorIndex;
                                        curr[X - 1, Y - 1] = colorIndex;
                                        curr[X - 2, Y - 1] = colorIndex;
                                        curr[X - 2, Y] = colorIndex;
                                    }
                                }

                                //ㅍ①
                                //  ②
                                //④③
                                if (
                                    0 <= Y - 2 &&
                                    sizeX > X + 1 &&

                                    (curr[X + 1, Y] == colorIndex) &&      // 1
                                    ( curr[X + 1, Y - 1] == colorIndex) &&  // 2
                                    ( curr[X + 1, Y - 2] == colorIndex) &&  // 3
                                    curr[X, Y - 2] == colorIndex                            // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X + 1, Y] = num;
                                    curr[X + 1, Y - 1] = num;
                                    curr[X + 1, Y - 2] = num;
                                    curr[X, Y - 2] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X + 1, Y] = colorIndex;
                                        curr[X + 1, Y - 1] = colorIndex;
                                        curr[X + 1, Y - 2] = colorIndex;
                                        curr[X, Y - 2] = colorIndex;
                                    }
                                }

                                //①②③
                                //ㅍ  ④
                                if (
                                    sizeX > X + 2 &&
                                    sizeY > Y + 1 &&

                                    ( curr[X, Y + 1] == colorIndex) &&      // 1
                                    ( curr[X + 1, Y + 1] == colorIndex) &&  // 2
                                    ( curr[X + 2, Y + 1] == colorIndex) &&  // 3
                                    curr[X + 2, Y] == colorIndex                            // 4
                                   )
                                {
                                    // PT5의 모형만큼의 범위를 모두 PT5으로 등록
                                    curr[X, Y] = num;
                                    curr[X, Y + 1] = num;
                                    curr[X + 1, Y + 1] = num;
                                    curr[X + 2, Y + 1] = num;
                                    curr[X + 2, Y] = num;
                                    // 블록 상태를 사용으로 전환
                                    wbm_List[set].waitBlockCheckList[15] = true;

                                    int nextCount = posListCount + 1;
                                    bool isCheck = false;

                                    // 현재 지정된 색상의 블록 수만큼 반복
                                    // 현재 맵에서 지정된 색상의 블록들 중 같은 색상의 블록을 탐색
                                    // 예시) 같은 색상의 5개 블록 중 4개가 사용 되었으면 나머지 하나의 위치도 확인하여 해당 자리도 블록이 위치 할 수 있는지 확인한다.
                                    //       즉, 지정된 색상의 모든 블록의 색상 값이 num + set값으로 변경되지 않았다면 다시 체크한다.
                                    for (int i = 0; i < posList.Count; i++)
                                    {
                                        if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
                                        {
                                            nextCount = i;
                                            isCheck = true;
                                            break;
                                        }
                                    }

                                    // 아직 남은 색상이 존재하면 다시 체크
                                    if (isCheck == false)
                                    {
                                        //Debug.Log("만세 클리어!");
                                        return true;
                                    }

                                    // 남은 부분이 블록으로 커버가 가능하면 리턴
                                    if (ReadingPatternCheck(nextCount))
                                    {
                                        return true;
                                    }
                                    // 커버가 불가능하면 정답 성립 불가능
                                    else
                                    {
                                        // 사용했던 블록 초기화
                                        wbm_List[set].waitBlockCheckList[15] = false;

                                        // 블록 값 초기화
                                        curr[X, Y] = colorIndex;
                                        curr[X, Y + 1] = colorIndex;
                                        curr[X + 1, Y + 1] = colorIndex;
                                        curr[X + 2, Y + 1] = colorIndex;
                                        curr[X + 2, Y] = colorIndex;
                                    }
                                }

                                #endregion
                            }
                        }
                        break;
                }
            }
        }

        // 블록이 남으면 탐색 실패로 반환
        for (int i = 0; i < posList.Count; i++)
        {
            if (curr[(int)posList[i].x, (int)posList[i].y] == colorIndex)
            {
                return false;
            }
        }

        // 블록이 남지 않으면 탐색 성공 반환
        return true;
    }
    public void InitOriBlockList()
    {
        originalBlockList.Add(new Block()
        {
            Num = 1,
            Name = "AM1",
            Width = 1,
            Height = 1,
            Squares = 1,
            Limit = -1,
            Map = new int[,]
            {
               { 1 }
            }
        }); // 1    AM1
        originalBlockList.Add(new Block()
        {
            Num = 2,
            Name = "D1",
            Width = 2,
            Height = 1,
            Squares = 2,
            Limit = -1,
            Map = new int[,]
            {
               { 1, 1 }
            }
        }); // 2    D1
        originalBlockList.Add(new Block()
        {
            Num = 3,
            Name = "TR1",
            Width = 3,
            Height = 1,
            Squares = 3,
            Limit = 25,
            LimitException = 3,
            Map = new int[,]
            {
               { 1, 1, 1 }
            }
        }); // 3    TR1
        originalBlockList.Add(new Block()
        {
            Num = 4,
            Name = "TR2",
            Width = 2,
            Height = 2,
            Squares = 3,
            Limit = 25,
            LimitException = 3,
            Map = new int[,]
            {
               { 1, 1 },
               { 0, 1 }
            }
        }); // 4    TR2
        originalBlockList.Add(new Block()
        {
            Num = 5,
            Name = "TE1",
            Width = 4,
            Height = 1,
            Squares = 4,
            Limit = -1,
            Map = new int[,]
            {
               { 1, 1, 1, 1 }
            }
        }); // 5    TE1
        originalBlockList.Add(new Block()
        {
            Num = 6,
            Name = "TE2",
            Width = 3,
            Height = 2,
            Squares = 4,
            Limit = -1,
            Map = new int[,]
            {
               { 1, 1, 1 },
               { 0, 0, 1 }
            }
        }); // 6    TE2
        originalBlockList.Add(new Block()
        {
            Num = 7,
            Name = "TE3",
            Width = 3,
            Height = 2,
            Squares = 4,
            Limit = -1,
            Map = new int[,]
            {
               { 1, 1, 1 },
               { 1, 0, 0 }
            }
        }); // 7    TE3
        originalBlockList.Add(new Block()
        {
            Num = 8,
            Name = "TE4",
            Width = 2,
            Height = 2,
            Squares = 4,
            Limit = -1,
            Map = new int[,]
            {
               { 1, 1 },
               { 1, 1 }
            }
        }); // 8    TE4
        originalBlockList.Add(new Block()
        {
            Num = 9,
            Name = "TE5",
            Width = 3,
            Height = 2,
            Squares = 4,
            Limit = -1,
            Map = new int[,]
            {
               { 0, 1, 1 },
               { 1, 1, 0 }
            }
        }); // 9    TE5
        originalBlockList.Add(new Block()
        {
            Num = 10,
            Name = "TE6",
            Width = 3,
            Height = 2,
            Squares = 4,
            Limit = -1,
            Map = new int[,]
            {
               { 1, 1, 1 },
               { 0, 1, 0 }
            }
        }); // 10   TE6
        originalBlockList.Add(new Block()
        {
            Num = 11,
            Name = "TE7",
            Width = 3,
            Height = 2,
            Squares = 4,
            Limit = -1,
            Map = new int[,]
            {
               { 1, 1, 0 },
               { 0, 1, 1 }
            }
        }); // 11   TE7
        originalBlockList.Add(new Block()
        {
            Num = 12,
            Name = "PT1",
            Width = 3,
            Height = 3,
            Squares = 5,
            Limit = -1,
            Map = new int[,]
            {
               { 1, 1, 1 },
               { 0, 1, 0 },
               { 0, 1, 0 }
            }
        }); // 12   PT1
        originalBlockList.Add(new Block()
        {
            Num = 13,
            Name = "PT2",
            Width = 3,
            Height = 3,
            Squares = 5,
            Limit = -1,
            Map = new int[,]
            {
               { 0, 1, 0 },
               { 1, 1, 1 },
               { 0, 1, 0 }
            }
        }); // 13   PT2
        originalBlockList.Add(new Block()
        {
            Num = 14,
            Name = "PT3",
            Width = 3,
            Height = 3,
            Squares = 5,
            Limit = -1,
            Map = new int[,]
            {
               { 1, 0, 0 },
               { 1, 1, 0 },
               { 0, 1, 1 }
            }
        }); // 14   PT3
        originalBlockList.Add(new Block()
        {
            Num = 15,
            Name = "PT4",
            Width = 3,
            Height = 3,
            Squares = 5,
            Limit = -1,
            Map = new int[,]
            {
               { 1, 1, 1 },
               { 1, 0, 0 },
               { 1, 0, 0 }
            }
        }); // 15   PT4
        originalBlockList.Add(new Block()
        {
            Num = 16,
            Name = "PT5",
            Width = 2,
            Height = 3,
            Squares = 5,
            Limit = -1,
            Map = new int[,]
            {
               { 1, 1 },
               { 1, 0 },
               { 1, 1 }
            }
        }); // 16   PT5
    }

    //SetToMathNum
    public void SetMatchToNumOfChange(int num)
    {
        ResetButtons();

        MatchToSetNum[num + 1] = MatchDropDown[num].value + 1;
        //Debug.Log(MatchToSetNum[num + 1] + "<>" + MatchDropDown[num].value + 1);
    }

    /// <summary>
    /// 왼편 중단에 표시되는 색상 블록 정보(블록 개수, 요구 세트 수) 버튼 초기화 함수
    /// </summary>
    public void InitMatchToNum()
    {
        // MatchToSetNum은 각 색상 버튼의 세트 수를 의미 한다.
        // 해당 배열은 색상 순서(노, 주, 초, 파, 빨, 갈, 흰, 검. 히. 남. 하늘, 핫핑크, 진남색)대로 있으며
        // 첫 번째 원소는 왜 굳이 존재하는지 아직은 모르겠다. 1세칸인데 14칸으로 1칸부터 사용한다.
        for (int i = 0; i < MatchToSetNum.Length; i++)
        {
            MatchToSetNum[i] = 1;
        }

        // 왼편 중단의 색상 버튼의 각 색상별 현재 블록의 개수를 초기화한다
        for (int i = 0; i < ColorWindow.Length; i++)
        {
            ColorWindow[i].text = "0";
        }
    }

    /// <summary>
    /// 버튼 리셋 함수
    /// </summary>
    public void ResetButtons()
    {
        CancelButtonCheck();

        for (int i = 0; i < LOGLIST.Count; i++)
        {
            LOGLIST[i].logObj.transform.GetChild(1).gameObject.SetActive(false);
        }

        for (int i = 0; i < 16; i++)
        {
            WB_PANEL_ITEMS[i].gameObject.SetActive(false);
            WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text = 0.ToString();
        }

        LOGTT_Button.gameObject.SetActive(false);
        LINE_MANAGER.gameObject.SetActive(false);
        isOKCheck = false;

        isCheckPixel_5 = false;
        isCheckPixel_4 = false;
        isCheckPixel_3 = false;

        BTN_1.interactable = true;
        BTN_2.interactable = false;
        BTN_OK.gameObject.SetActive(false);

        if (GAM != null)
        {
            for (int i = 0; i < GAM.transform.childCount; i++)
            {
                Color newColor = new Color(Random.value, Random.value, Random.value);
                for (int j = 0; j < GAM.transform.GetChild(i).transform.childCount; j++)
                {
                    GAM.transform.GetChild(i).transform.GetChild(j).gameObject.transform.GetChild(1).gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < GAM.transform.childCount; i++)
            {
                for (int j = GAM.transform.GetChild(i).childCount - 1; j >= 0; j--)
                {
                    GAM.transform.GetChild(i).transform.GetChild(j).transform.parent = MapManager.S.mapPivot.transform;
                }
            }

            Destroy(GAM);
        }
    }

    /// <summary>
    /// 모든 블록의 네칸, 세칸 탐색 초기화
    /// </summary>
    public void ResetAllBranch()
    {
        for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
        {
            for (int j = 0; j < MapManager.S.mapPixel_X; j++)
            {
                if (MapManager.S.Pattern_GO[j, i].gameObject.GetComponent<Pixel>().isBranch != -1)
                {
                    MapManager.S.Pattern_GO[j, i].gameObject.GetComponent<Pixel>().isBranch = -1;
                    MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(0).gameObject.SetActive(false);
                    MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(2).gameObject.SetActive(false);
                }
            }
        }
    }

    public void OK_BUTTON()
    {
        string result = "";

        int pickIndex = -1;
        for (int i = 0; i < LOGLIST.Count; i++)
        {
            if(LOGLIST[i].colorIndex == Temp_colorIndex)
            {
                pickIndex = i;
                break;
            }
        }

        if(pickIndex != -1)
        {
            GameObject log = LOGLIST[pickIndex].logObj;

            Destroy(LOGLIST[pickIndex].lineItem.gameObject);
            LOGLIST.RemoveAt(pickIndex);

            Destroy(log);
        }

        switch (Temp_colorIndex)
        {
            case 1:
                result += " YE\t";
                break;

            case 2:
                result += " OR\t";
                break;

            case 3:
                result += " GN\t";
                break;

            case 4:
                result += " BU\t";
                break;

            case 5:
                result += " RD\t";
                break;

            case 6:
                result += " BR\t";
                break;

            case 7:
                result += " WH\t";
                break;

            case 8:
                result += " BK\t";
                break;

            case 9:
                result += " GY\t";
                break;

            case 10:
                result += " NV\t";
                break;

            case 11:
                result += " SB\t";
                break;

            case 12:
                result += " HP\t";
                break;

            case 13:
                result += "DB\t";
                break;
        }

        result += " / [Pixel :" + Temp_pixelCount;
        result += "] / [Set : " + Temp_colorSetCount + "] / ";
        result += "<<Chance : "  + (ConstrastColorMapNumOf) + ">>";

        GameObject msg = Instantiate(log_prefab_, null);
        msg.transform.GetChild(0).gameObject.GetComponent<Text>().text = result;
        msg.transform.parent = content_.transform;
        msg.transform.localScale = Vector3.one;
        msg.gameObject.transform.localPosition = Vector3.zero;

        LOGLIST.Add(new LOG());
        LOGLIST[LOGLIST.Count - 1].colorIndex           = Temp_colorIndex;
        LOGLIST[LOGLIST.Count - 1].logObj               = msg;
        LOGLIST[LOGLIST.Count - 1].LogHistory_isActive  = new bool[MapManager.S.mapPixel_X, MapManager.S.mapPixel_Y];
        LOGLIST[LOGLIST.Count - 1].LogHistory_myColor   = new Color[MapManager.S.mapPixel_X, MapManager.S.mapPixel_Y];
        LOGLIST[LOGLIST.Count - 1].LogHistory_myNumText = new string[MapManager.S.mapPixel_X, MapManager.S.mapPixel_Y];

        LOGLIST[LOGLIST.Count - 1].WB_NUMOF_LIST.AddRange(WaitBlockNumOfList);

        for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
        {
            for (int j = 0; j < MapManager.S.mapPixel_X; j++)
            {
                if (MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(1).gameObject.activeSelf == false)
                {
                    //Debug.Log("LogListCount : " + LOGLIST[LOGLIST.Count - 1].LogHistory.Length);
                    LOGLIST[LOGLIST.Count - 1].LogHistory_isActive[j, i] = false;
                }
                else
                {
                    LOGLIST[LOGLIST.Count - 1].LogHistory_isActive[j, i]    = true;
                    LOGLIST[LOGLIST.Count - 1].LogHistory_myColor[j, i]     = MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().color;
                    LOGLIST[LOGLIST.Count - 1].LogHistory_myNumText[j, i]   = MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(1).gameObject.transform.GetChild(0).gameObject.GetComponent<TextMesh>().text;
                }
            }
        }

        LOGLIST[LOGLIST.Count - 1].lineItem = LINE_MANAGER.transform.GetChild(LINE_MANAGER.transform.childCount - 1).gameObject;

        ResetButtons();
    }

    /// <summary>
    /// 왼편 중단 컬러 세트 버튼이 활성화될때 생기는 체크 표시 및 
    /// </summary>
    /// <param name="_colorIndex"></param>
    public void ChangeCalcColorWindow(int _colorIndex)
    {
        ResetButtons();
        colorIndex = _colorIndex + 1;

        for (int i = 0; i < ColorCalcWindow.Length; i++)
        {
            if (i == _colorIndex)
            {
                ColorCalcWindow[i] = true;
                ColorCalcButton[i].gameObject.transform.GetChild(2).gameObject.SetActive(true);
            }
            else
            {
                ColorCalcWindow[i] = false;
                ColorCalcButton[i].gameObject.transform.GetChild(2).gameObject.SetActive(false);
            }
        }

        ConstrastColorMapsList.Clear();
        ConstrastColorMapNumOf = 0;

        CanvasColorDropDownMenu.value = _colorIndex;
    }

    public void ActiveLogPrefab(GameObject _target)
    {
        if(isOKCheck == false)
        {
            for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
            {
                for (int j = 0; j < MapManager.S.mapPixel_X; j++)
                {
                    MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(1).gameObject.SetActive(false);
                }
            }

            BTN_1.interactable = false;
            BTN_2.interactable = false;

            isOKCheck = true;

            LOGTT_Button.gameObject.SetActive(true);
        }
        
        int fIndex = -1;

        for (int i = 0; i < LOGLIST.Count; i++)
        {
            if(_target == LOGLIST[i].logObj)
            {
                fIndex = i;
                break;
            }
        }

        if(fIndex == -1)
        {
            Debug.Log("해당 로그를 찾지못함!");
            return;
        }
        else
        {
            //TURN OFF
            if (LOGLIST[fIndex].lineItem.gameObject.activeSelf)
            {
                for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
                {
                    for (int j = 0; j < MapManager.S.mapPixel_X; j++)
                    {
                        if (LOGLIST[fIndex].LogHistory_isActive[j, i] == false)
                        {
                            //MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(1).gameObject.SetActive(false);
                        }
                        else
                        {
                            MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(1).gameObject.SetActive(false);
                        }
                    }
                }

                LOGLIST[fIndex].lineItem.gameObject.SetActive(false);
                _target.transform.GetChild(1).gameObject.SetActive(false);

                //WB LIST PRINT..!
                for (int i = 0; i < 16; i++)
                {
                    WB_PANEL_ITEMS[i].gameObject.SetActive(true);
                    WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text = (int.Parse(WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text) - LOGLIST[fIndex].WB_NUMOF_LIST[i]).ToString();
                }

                for (int i = 0; i < 16; i++)
                {
                    if ((int.Parse(WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text) == 0))
                    {
                        WB_PANEL_ITEMS[i].gameObject.SetActive(false);
                        WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text = 0.ToString();
                    }
                }

                bool isEnd = false;
                for (int i = 0; i < LOGLIST.Count; i++)
                {
                    if (LOGLIST[i].lineItem.gameObject.activeSelf)
                    {
                        isEnd = true;
                        break;
                    }
                }

                if (isEnd == false)
                    ResetButtons();
            }
            //TURN ON
            else
            {
                //Debug.Log("fIndex : " + fIndex);

                for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
                {
                    for (int j = 0; j < MapManager.S.mapPixel_X; j++)
                    {
                        if (LOGLIST[fIndex].LogHistory_isActive[j, i] == false)
                        {
                            //MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(1).gameObject.SetActive(false);
                        }
                        else
                        {
                            MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(1).gameObject.SetActive(true);
                            MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(0).gameObject.GetComponent<SpriteRenderer>().color = LOGLIST[fIndex].LogHistory_myColor[j, i];
                            MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(1).gameObject.transform.GetChild(0).gameObject.GetComponent<TextMesh>().text = LOGLIST[fIndex].LogHistory_myNumText[j, i];
                        }
                    }
                }

                LINE_MANAGER.gameObject.SetActive(true);
                LOGLIST[fIndex].lineItem.gameObject.SetActive(true);
                _target.transform.GetChild(1).gameObject.SetActive(true);

                //WB LIST PRINT..!
                int limit = (Option_PentoMode.isOn) ? 16 : 11;
                for (int i = 0; i < limit; i++)
                {
                    WB_PANEL_ITEMS[i].gameObject.SetActive(true);
                    WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text = (int.Parse(WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text) + LOGLIST[fIndex].WB_NUMOF_LIST[i]).ToString();
                }

                for (int i = 0; i < limit; i++)
                {
                    if ((int.Parse(WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text) == 0))
                    {
                        WB_PANEL_ITEMS[i].gameObject.SetActive(false);
                        WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text = 0.ToString();
                    }
                }
            }
        }
    }
    public void CancelButtonCheck()
    {
        for (int i = 0; i < 16; i++)
        {
            WB_PANEL_ITEMS[i].gameObject.SetActive(false);
            WB_PANEL_ITEMS[i].transform.GetChild(0).gameObject.GetComponent<Text>().text = 0.ToString();
        }

        for (int i = 0; i < LOGLIST.Count; i++)
        {
            LOGLIST[i].logObj.transform.GetChild(1).gameObject.SetActive(false);
        }

        for (int i = 0; i < MapManager.S.mapPixel_Y; i++)
        {
            for (int j = 0; j < MapManager.S.mapPixel_X; j++)
            {
                MapManager.S.Pattern_GO[j, i].gameObject.transform.GetChild(1).gameObject.SetActive(false);
            }
        }

        LOGTT_Button.gameObject.SetActive(false);
        isOKCheck = false;

        isCheckPixel_4 = false;
        isCheckPixel_3 = false;

        BTN_1.interactable = true;
        BTN_2.interactable = false;
        BTN_OK.gameObject.SetActive(false);

        LINE_MANAGER.gameObject.SetActive(false);
        for (int i = 0; i < LINE_MANAGER.transform.childCount; i++)
        {
            LINE_MANAGER.transform.GetChild(i).gameObject.SetActive(false);
        }
        
    }

    public void InitLogData()
    {
        //Debug.Log("InitLogData");

        for (int i = LOGLIST.Count - 1; i >= 0; i--)
        {
            Destroy(LOGLIST[i].logObj);
            Destroy(LOGLIST[i].lineItem);

            LOGLIST.RemoveAt(i);
        }
    }

    /// <summary>
    /// 업데이트 : Q 블록 계산 시작
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
        {
            if(IF_InputField.text != "")
            {
                mTime = int.Parse(IF_InputField.text) * 1000.0f;
                IF_InputField.placeholder.GetComponent<Text>().text = "Timer : " + IF_InputField.text + " Sec";
                IF_InputField.Select();
                IF_InputField.text = "";
            }
        }

        // Q를 한번 클릭할때마다 네칸, 세칸, 기본 탐색을 한다.
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (BTN_1.interactable || BTN_2.interactable)
            {
                myResultText.text = "계산중..";
            }
            else if (BTN_OK.gameObject.activeSelf)
            {
                myResultText.text = "";
            }
            else if (LOGTT_Button.gameObject.activeSelf)
            {
                myResultText.text = "계산중..";
            }

            // 이전에 동작하는 알고리즘 취소 및 새 알고리즘 시작
            CancelInvoke("CalcButtonStateCheckProcess");
            Invoke("CalcButtonStateCheckProcess", 0.1f);
        }
    }

    public void CalcButtonStateCheckProcess()
    {
        if (BTN_1.interactable || BTN_2.interactable)
        {
            if (isTimeOver)
            {
                OK_BUTTON();
                CancelButtonCheck();
                isTimeOver = false;
            }

            ReadingPatternColorCheck();
        }
        else if (BTN_OK.gameObject.activeSelf)
        {
            OK_BUTTON();
        }
        else if (LOGTT_Button.gameObject.activeSelf)
        {
            CancelButtonCheck();
        }
        else
        {
            BTN_1.interactable = true;
            BTN_2.interactable = false;

            myResultText.text = "";
        }
    }

    //Options
    public void Option_MirrorModeActive()
    {
        Debug.Log("Option_MirrorModeActive()");

        //Debug.Log("FullSize : " + MapManager.S.mapPixel_X + "," + MapManager.S.mapPixel_Y);
        int _harfSizeX = MapManager.S.mapPixel_X / 2;

        //Debug.Log("HarfXSize : " + _harfSizeX);

        int[,] _harfMapColorIndexArrary = new int[_harfSizeX, MapManager.S.mapPixel_Y]; 
        for (int y = 0; y < MapManager.S.mapPixel_Y; y++)
        {
            for (int x = 0; x < _harfSizeX; x++)
            {
                _harfMapColorIndexArrary[x, y] = MapManager.S.Pattern_GO[x, y].gameObject.GetComponent<Pixel>().currCount;
            }
        }

        //string allStr = "";
        //for (int y = 0; y < MapManager.S.mapPixel_Y; y++)
        //{
        //    for (int x = 0; x < _harfSizeX; x++)
        //    {
        //        allStr += _harfMapColorIndexArrary[x, y];
        //        allStr += " | ";
        //    }
        //    allStr += "\n";
        //}

        //Debug.Log(allStr);


        for (int y = 0; y < MapManager.S.mapPixel_Y; y++)
        {
            for (int x = MapManager.S.mapPixel_X -1; x >= _harfSizeX; x--)
            {
                int _changeColorIndex = _harfMapColorIndexArrary[(MapManager.S.mapPixel_X - 1) - x, y];
                MapManager.S.Pattern_GO[x, y].gameObject.GetComponent<Pixel>().ChangeColor(_changeColorIndex);
            }
        }
    }
}
