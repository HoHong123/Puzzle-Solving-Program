using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapManager : MonoBehaviour {

    public enum CURRENT_MAPSIZE
    {
        NONE,
        P_88,
        P_816,
        P_168,
        P_1616,
        P_1624,
        P_2416,
        P_2424
    }
    public CURRENT_MAPSIZE currMapSize = CURRENT_MAPSIZE.NONE;
    public static MapManager S = null;

    public GameObject mapPivot = null;
    public GameObject mapPixel = null;

    [Range(0, 10)]
    public float mapPixel_Offset = 0.0f;

    public int mapPixel_X = 0;
    public int mapPixel_Y = 0;

    [Header("Select Material Buttons")]
    public int          SelectToMatIndex    = -1;
    public Material[]   MatchToMaterials    = null;

    [Space(10)]
    public Material     EaserMat            = null;

    [Header("Reading Pattern")]
    public int[,]           Pattern_Map = null;
    public GameObject[,]    Pattern_GO  = null;

    [Header("Drag Point")]
    public bool isDrag = false;

    private float x__ = -4.75f;
    private float y__ = -4.3f;
    [Header("Resolution Value")]
    [Range(0, 5)]
    public int resolVal = 0;

    [Header("LINE_MAT")]
    public Material line_Mat = null;
    //private Gradient            grad;
    //private GradientColorKey[]  colorKey;
    //private GradientAlphaKey[]  alphaKey;

    [Header("DROPDOWN_MENU")]
    public Dropdown Canvas_Size_DropDown_Menu   = null;
    public Dropdown Canvas_Color_DropDown_Menu  = null;

    [Header("USE FONT")]
    public Font Map_UseFont = null;

    [Header("Draw Straight Line")]
    public Vector2 DSL_StartVec = new Vector2(-1, -1);
    public Vector2 DSL_EndVec   = new Vector2(-1, -1);

    public class PatternMapClass
    {
        public int[,]   Pattern_Map         = null;
        public int[,]   Pattern_GO          = null;
        public int[,]   Pattern_Branch      = null;
        public int      CurrCalcColorIndex  = -1;
    }

    [Header("UNDO - STACK")]
    public List<PatternMapClass> UNDO_LIST = new List<PatternMapClass>();

    [Header("BRANCH INFO")]
    public bool     isPixelBranch_4 = false;
    public bool     isPixelBranch_3 = false;
    public Button   Branch4Pixel    = null;
    public Button   Branch3Pixel    = null;

    private void Awake()
    {
        S = this;

        //grad = new Gradient();

        //colorKey = new GradientColorKey[2];
        //colorKey[0].color = Color.gray;
        //colorKey[0].time = 0.0f;
        //colorKey[1].color = Color.gray;
        //colorKey[1].time = 1.0f;

        //alphaKey = new GradientAlphaKey[2];
        //alphaKey[0].alpha = 1.0f;
        //alphaKey[0].time = 0.0f;
        //alphaKey[1].alpha = 1.0f;
        //alphaKey[1].time = 1.0f;

        //grad.SetKeys(colorKey, alphaKey);
    }

    private void Start()
    {
        ActiveMaterial(0);
        Size_88();
    }

    #region 맵 만드는 함수들 (맵 박스 오브젝트 및 아웃라인과 각 줄에 출력되는 번호까지)
    public void Size_88()
    {
        // 왼쪽 중단에 나오는 색상 정보(블록 개수, 필요세트) 버튼 비활성화
        for (int i = 0; i < ReadingPatternManager.S.ColorCalcButton.Length; i++)
        {
            ReadingPatternManager.S.ColorCalcButton[i].gameObject.SetActive(false);
        }

        UNDO_LIST.Clear(); // 되돌리기 기능 스택 제거
        ReadingPatternManager.S.InitMatchToNum(); // 왼쪽 블록 색상, 세트 버튼 UI 초기화

        // 카메라 위치 초기화
        Camera.main.transform.position = new Vector3(x__, y__, -10);
        Camera.main.orthographicSize = 10;

        // 색을 각 블록 오브젝트 제거
        // 도안 크기별 첫 번째 좌표부터 마지막까지 도형의 위치 및 정보를 다시 받아야하기 때문
        for (int i = 0; i < mapPivot.transform.childCount; i++)
        {
            Destroy(mapPivot.transform.GetChild(i).gameObject);
        }

        mapPixel_X      = 8; mapPixel_Y = 8; // x, y 크기
        Pattern_Map     = new int[mapPixel_X, mapPixel_Y];          // 맵 정보 배열
        Pattern_GO      = new GameObject[mapPixel_X, mapPixel_Y];   // 맵 오브젝트 배열
        Vector3 newPos  = mapPivot.transform.position; // 맵의 가장 좌측 하단(시작자리) 자리 위치 할당

        // 맵 생성
        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                // Pixel 프리팹을 생성하여 Pivot을 부모로 위치 지정
                GameObject pixel = Instantiate(mapPixel, mapPivot.transform);
                pixel.transform.position = newPos; // 좌측 하단으로 일단 이동

                // 각 위치별 이동
                newPos = new Vector3(newPos.x + mapPixel_Offset, newPos.y, newPos.z);
                Pattern_Map[j, i]   = 1;        // 픽셀 정보 넣기
                Pattern_GO[j, i]    = pixel;    // 오브젝트 등록

                // 각 오브젝트의 현재 위치 등록
                pixel.gameObject.GetComponent<Pixel>().my_X = j;
                pixel.gameObject.GetComponent<Pixel>().my_Y = i;

                // 왼편 첫번째 블록이면 왼편에 현재 줄의 가로 번호를 출력하기 위한 텍스트 생성
                if (j == 0)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(-1, 0, 0);
                    myMesh.text = ((mapPixel_Y) - i).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material; 

                    myMesh.anchor       = TextAnchor.MiddleCenter;
                    myMesh.alignment    = TextAlignment.Center;
                    myMesh.fontSize     = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (i == 0 || (i + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.gameObject.name = "OutLine";
                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (i != 0)
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, mapPixel_Offset * 0.5f, 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(mapPixel_X * mapPixel_Offset, 0, 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
                // 최상단 블록이면 현재 열의 세로 번호를 출력하기 위한 텍스트 생성
                if (i == mapPixel_Y - 1)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(0, 1, 0);
                    myMesh.text = (j + 1).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (j == 0 || (j + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (j != 0)
                        {
                            newPos2 -= new Vector3(-(mapPixel_Offset * 0.5f), -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(0, -(mapPixel_Y * mapPixel_Offset), 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
            }

            // 새 위치로 이동
            newPos = new Vector3(mapPivot.transform.position.x, newPos.y + mapPixel_Offset, newPos.z);
        }

        currMapSize = CURRENT_MAPSIZE.P_88;
    }

    public void Size_816()
    {
        for (int i = 0; i < ReadingPatternManager.S.ColorCalcButton.Length; i++)
        {
            ReadingPatternManager.S.ColorCalcButton[i].gameObject.SetActive(false);
        }

        UNDO_LIST.Clear();
        ReadingPatternManager.S.InitMatchToNum();

        // 신규 맵에 따라 카메라 위치 조절
        Camera.main.transform.position = new Vector3(x__ + resolVal, y__, -10);
        Camera.main.orthographicSize = 10;

        for (int i = 0; i < mapPivot.transform.childCount; i++)
        {
            Destroy(mapPivot.transform.GetChild(i).gameObject);
        }

        mapPixel_X      = 16; mapPixel_Y = 8;
        Pattern_Map     = new int[mapPixel_X, mapPixel_Y];
        Pattern_GO      = new GameObject[mapPixel_X, mapPixel_Y];
        Vector3 newPos  = mapPivot.transform.position;

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                GameObject pixel = Instantiate(mapPixel, mapPivot.transform);
                pixel.transform.position = newPos;

                newPos = new Vector3(newPos.x + mapPixel_Offset, newPos.y, newPos.z);
                Pattern_Map[j, i] = 1;
                Pattern_GO[j, i] = pixel;

                pixel.gameObject.GetComponent<Pixel>().my_X = j;
                pixel.gameObject.GetComponent<Pixel>().my_Y = i;

                if (j == 0)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(-1, 0, 0);
                    myMesh.text = ((mapPixel_Y) - i).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (i == 0 || (i + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (i != 0)
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, mapPixel_Offset * 0.5f, 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(mapPixel_X * mapPixel_Offset, 0, 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
                if (i == mapPixel_Y - 1)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(0, 1, 0);
                    myMesh.text = (j + 1).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (j == 0 || (j + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (j != 0)
                        {
                            newPos2 -= new Vector3(-(mapPixel_Offset * 0.5f), -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(0, -(mapPixel_Y * mapPixel_Offset), 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
            }
            newPos = new Vector3(mapPivot.transform.position.x, newPos.y + mapPixel_Offset, newPos.z);
        }

        currMapSize = CURRENT_MAPSIZE.P_816;
    }

    public void Size_168()
    {
        for (int i = 0; i < ReadingPatternManager.S.ColorCalcButton.Length; i++)
        {
            ReadingPatternManager.S.ColorCalcButton[i].gameObject.SetActive(false);
        }

        UNDO_LIST.Clear();
        ReadingPatternManager.S.InitMatchToNum();

        Camera.main.transform.position = new Vector3(x__, y__ + resolVal, -10);
        Camera.main.orthographicSize = 10;

        for (int i = 0; i < mapPivot.transform.childCount; i++)
        {
            Destroy(mapPivot.transform.GetChild(i).gameObject);
        }

        mapPixel_X = 8; mapPixel_Y = 16;
        Pattern_Map = new int[mapPixel_X, mapPixel_Y];
        Pattern_GO = new GameObject[mapPixel_X, mapPixel_Y];
        Vector3 newPos = mapPivot.transform.position;

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                GameObject pixel = Instantiate(mapPixel, mapPivot.transform);
                pixel.transform.position = newPos;

                newPos = new Vector3(newPos.x + mapPixel_Offset, newPos.y, newPos.z);
                Pattern_Map[j, i] = 1;
                Pattern_GO[j, i] = pixel;

                pixel.gameObject.GetComponent<Pixel>().my_X = j;
                pixel.gameObject.GetComponent<Pixel>().my_Y = i;

                if (j == 0)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(-1, 0, 0);
                    myMesh.text = ((mapPixel_Y) - i).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (i == 0 || (i + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (i != 0)
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, mapPixel_Offset * 0.5f, 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(mapPixel_X * mapPixel_Offset, 0, 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
                if (i == mapPixel_Y - 1)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(0, 1, 0);
                    myMesh.text = (j + 1).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (j == 0 || (j + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (j != 0)
                        {
                            newPos2 -= new Vector3(-(mapPixel_Offset * 0.5f), -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(0, -(mapPixel_Y * mapPixel_Offset), 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
            }
            newPos = new Vector3(mapPivot.transform.position.x, newPos.y + mapPixel_Offset, newPos.z);
        }

        currMapSize = CURRENT_MAPSIZE.P_168;
    }

    public void Size_1616()
    {
        for (int i = 0; i < ReadingPatternManager.S.ColorCalcButton.Length; i++)
        {
            ReadingPatternManager.S.ColorCalcButton[i].gameObject.SetActive(false);
        }

        UNDO_LIST.Clear();
        ReadingPatternManager.S.InitMatchToNum();

        Camera.main.transform.position = new Vector3(x__ + resolVal, y__ + resolVal, -10);
        Camera.main.orthographicSize = 10;

        for (int i = 0; i < mapPivot.transform.childCount; i++)
        {
            Destroy(mapPivot.transform.GetChild(i).gameObject);
        }

        mapPixel_X      = 16; mapPixel_Y = 16;
        Pattern_Map     = new int[mapPixel_X, mapPixel_Y];
        Pattern_GO      = new GameObject[mapPixel_X, mapPixel_Y];
        Vector3 newPos  = mapPivot.transform.position;

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                GameObject pixel = Instantiate(mapPixel, mapPivot.transform);
                pixel.transform.position = newPos;

                newPos = new Vector3(newPos.x + mapPixel_Offset, newPos.y, newPos.z);
                Pattern_Map[j, i] = 1;
                Pattern_GO[j, i] = pixel;

                pixel.gameObject.GetComponent<Pixel>().my_X = j;
                pixel.gameObject.GetComponent<Pixel>().my_Y = i;

                if (j == 0)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(-1, 0, 0);
                    myMesh.text = ((mapPixel_Y) - i).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (i == 0 || (i + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (i != 0)
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, mapPixel_Offset * 0.5f, 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(mapPixel_X * mapPixel_Offset, 0, 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
                if (i == mapPixel_Y - 1)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(0, 1, 0);
                    myMesh.text = (j + 1).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (j == 0 || (j + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (j != 0)
                        {
                            newPos2 -= new Vector3(-(mapPixel_Offset * 0.5f), -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(0, -(mapPixel_Y * mapPixel_Offset), 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
            }
            newPos = new Vector3(mapPivot.transform.position.x, newPos.y + mapPixel_Offset, newPos.z);
        }

        currMapSize = CURRENT_MAPSIZE.P_1616;
    }

    public void Size_1624()
    {
        for (int i = 0; i < ReadingPatternManager.S.ColorCalcButton.Length; i++)
        {
            ReadingPatternManager.S.ColorCalcButton[i].gameObject.SetActive(false);
        }

        UNDO_LIST.Clear();
        ReadingPatternManager.S.InitMatchToNum();

        Camera.main.transform.position = new Vector3(x__ + (resolVal * 2), y__ + resolVal, -10);
        Camera.main.orthographicSize = 11;

        for (int i = 0; i < mapPivot.transform.childCount; i++)
        {
            Destroy(mapPivot.transform.GetChild(i).gameObject);
        }

        mapPixel_X = 24; mapPixel_Y = 16;
        Pattern_Map = new int[mapPixel_X, mapPixel_Y];
        Pattern_GO = new GameObject[mapPixel_X, mapPixel_Y];
        Vector3 newPos = mapPivot.transform.position;

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                GameObject pixel = Instantiate(mapPixel, mapPivot.transform);
                pixel.transform.position = newPos;

                newPos = new Vector3(newPos.x + mapPixel_Offset, newPos.y, newPos.z);
                Pattern_Map[j, i] = 1;
                Pattern_GO[j, i] = pixel;

                pixel.gameObject.GetComponent<Pixel>().my_X = j;
                pixel.gameObject.GetComponent<Pixel>().my_Y = i;

                if (j == 0)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(-1, 0, 0);
                    myMesh.text = ((mapPixel_Y) - i).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (i == 0 || (i + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (i != 0)
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, mapPixel_Offset * 0.5f, 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(mapPixel_X * mapPixel_Offset, 0, 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
                if (i == mapPixel_Y - 1)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(0, 1, 0);
                    myMesh.text = (j + 1).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (j == 0 || (j + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (j != 0)
                        {
                            newPos2 -= new Vector3(-(mapPixel_Offset * 0.5f), -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(0, -(mapPixel_Y * mapPixel_Offset), 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
            }
            newPos = new Vector3(mapPivot.transform.position.x, newPos.y + mapPixel_Offset, newPos.z);
        }

        currMapSize = CURRENT_MAPSIZE.P_1624;
    }

    public void Size_2416()
    {
        for (int i = 0; i < ReadingPatternManager.S.ColorCalcButton.Length; i++)
        {
            ReadingPatternManager.S.ColorCalcButton[i].gameObject.SetActive(false);
        }

        UNDO_LIST.Clear();
        ReadingPatternManager.S.InitMatchToNum();

        Camera.main.transform.position = new Vector3(x__ + resolVal, y__ + (resolVal * 2), -10);
        Camera.main.orthographicSize = 13;

        for (int i = 0; i < mapPivot.transform.childCount; i++)
        {
            Destroy(mapPivot.transform.GetChild(i).gameObject);
        }

        mapPixel_X = 16; mapPixel_Y = 24;
        Pattern_Map = new int[mapPixel_X, mapPixel_Y];
        Pattern_GO = new GameObject[mapPixel_X, mapPixel_Y];
        Vector3 newPos = mapPivot.transform.position;

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                GameObject pixel = Instantiate(mapPixel, mapPivot.transform);
                pixel.transform.position = newPos;

                newPos = new Vector3(newPos.x + mapPixel_Offset, newPos.y, newPos.z);
                Pattern_Map[j, i] = 1;
                Pattern_GO[j, i] = pixel;

                pixel.gameObject.GetComponent<Pixel>().my_X = j;
                pixel.gameObject.GetComponent<Pixel>().my_Y = i;

                if (j == 0)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(-1, 0, 0);
                    myMesh.text = ((mapPixel_Y) - i).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (i == 0 || (i + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (i != 0)
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, mapPixel_Offset * 0.5f, 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(mapPixel_X * mapPixel_Offset, 0, 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
                if (i == mapPixel_Y - 1)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(0, 1, 0);
                    myMesh.text = (j + 1).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (j == 0 || (j + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (j != 0)
                        {
                            newPos2 -= new Vector3(-(mapPixel_Offset * 0.5f), -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(0, -(mapPixel_Y * mapPixel_Offset), 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
            }
            newPos = new Vector3(mapPivot.transform.position.x, newPos.y + mapPixel_Offset, newPos.z);
        }

        currMapSize = CURRENT_MAPSIZE.P_2416;
    }

    public void Size_2424()
    {
        for (int i = 0; i < ReadingPatternManager.S.ColorCalcButton.Length; i++)
        {
            ReadingPatternManager.S.ColorCalcButton[i].gameObject.SetActive(false);
        }

        UNDO_LIST.Clear();
        ReadingPatternManager.S.InitMatchToNum();

        Camera.main.transform.position = new Vector3(x__ + (resolVal * 2), y__ + (resolVal * 2), -10);
        Camera.main.orthographicSize = 13;

        for (int i = 0; i < mapPivot.transform.childCount; i++)
        {
            Destroy(mapPivot.transform.GetChild(i).gameObject);
        }

        mapPixel_X = 24; mapPixel_Y = 24;
        Pattern_Map = new int[mapPixel_X, mapPixel_Y];
        Pattern_GO = new GameObject[mapPixel_X, mapPixel_Y];
        Vector3 newPos = mapPivot.transform.position;

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                GameObject pixel = Instantiate(mapPixel, mapPivot.transform);
                pixel.transform.position = newPos;

                newPos = new Vector3(newPos.x + mapPixel_Offset, newPos.y, newPos.z);
                Pattern_Map[j, i] = 1;
                Pattern_GO[j, i] = pixel;

                pixel.gameObject.GetComponent<Pixel>().my_X = j;
                pixel.gameObject.GetComponent<Pixel>().my_Y = i;

                if (j == 0)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(-1, 0, 0);
                    myMesh.text = ((mapPixel_Y) - i).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (i == 0 || (i + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (i != 0)
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, mapPixel_Offset * 0.5f, 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(mapPixel_X * mapPixel_Offset, 0, 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
                if (i == mapPixel_Y - 1)
                {
                    GameObject maker = new GameObject();
                    maker.transform.parent = pixel.transform;

                    maker.AddComponent<TextMesh>();
                    TextMesh myMesh = maker.GetComponent<TextMesh>();

                    maker.transform.localPosition = new Vector3(0, 1, 0);
                    myMesh.text = (j + 1).ToString();

                    myMesh.font = Map_UseFont;
                    maker.GetComponentInChildren<MeshRenderer>().material = myMesh.font.material;

                    myMesh.anchor = TextAnchor.MiddleCenter;
                    myMesh.alignment = TextAlignment.Center;
                    myMesh.fontSize = 100;

                    if (int.Parse(myMesh.text) < 10)
                    {
                        myMesh.characterSize = 0.08f;
                    }
                    else
                    {
                        myMesh.characterSize = 0.07f;
                    }

                    if (j == 0 || (j + 1) % 8 == 0)
                    {
                        //Debug.Log("Count : " + i);
                        GameObject child = new GameObject();
                        child.transform.parent = mapPivot.transform.transform;

                        child.gameObject.AddComponent<LineRenderer>();
                        LineRenderer myLine = child.gameObject.GetComponent<LineRenderer>();

                        myLine.positionCount = 2;
                        myLine.startWidth = 0.015f;
                        myLine.endWidth = 0.015f;
                        myLine.material = line_Mat;
                        myLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

                        Vector3 newPos2 = Pattern_GO[j, i].transform.position;

                        if (j != 0)
                        {
                            newPos2 -= new Vector3(-(mapPixel_Offset * 0.5f), -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }
                        else
                        {
                            newPos2 -= new Vector3(mapPixel_Offset * 0.5f, -(mapPixel_Offset * 0.5f), 0);
                            newPos2.z = -5.0f;
                        }

                        myLine.SetPosition(0, newPos2);

                        newPos2 += new Vector3(0, -(mapPixel_Y * mapPixel_Offset), 0);
                        newPos2.z = -5.0f;

                        myLine.SetPosition(1, newPos2);
                    }
                }
            }
            newPos = new Vector3(mapPivot.transform.position.x, newPos.y + mapPixel_Offset, newPos.z);
        }

        currMapSize = CURRENT_MAPSIZE.P_2424;
    }
    #endregion

    public void ActiveMaterial(int _num)
    {
        SelectToMatIndex = _num;
        if (_num == -1) return;

        ReadingPatternManager.S.ResetButtons();
    }

    //UNDO
    public void AddHistory()
    {
        UNDO_LIST.Add(new PatternMapClass());
        UNDO_LIST[UNDO_LIST.Count - 1].Pattern_Map    = new int [mapPixel_X, mapPixel_Y];
        UNDO_LIST[UNDO_LIST.Count - 1].Pattern_GO     = new int [mapPixel_X, mapPixel_Y];
        UNDO_LIST[UNDO_LIST.Count - 1].Pattern_Branch = new int [mapPixel_X, mapPixel_Y];

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                UNDO_LIST[UNDO_LIST.Count - 1].Pattern_Map[j, i] = Pattern_Map[j, i];
            }
        }

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                UNDO_LIST[UNDO_LIST.Count - 1].Pattern_GO[j, i] = Pattern_GO[j, i].gameObject.GetComponent<Pixel>().currCount;
            }
        }

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                UNDO_LIST[UNDO_LIST.Count - 1].Pattern_Branch[j, i] = Pattern_GO[j, i].gameObject.GetComponent<Pixel>().isBranch;
            }
        }

        for (int i = 0; i < ReadingPatternManager.S.ColorCalcButton.Length; i++)
        {
            if (ReadingPatternManager.S.ColorCalcButton[i].gameObject.transform.GetChild(2).gameObject.activeSelf)
            {
                UNDO_LIST[UNDO_LIST.Count - 1].CurrCalcColorIndex = i;
                break;
            }
        }
        //Debug.Log("UNDO LIST COUNT : " + UNDO_LIST.Count);
    }
    public void UndoHistory()
    {
        if(UNDO_LIST.Count <= 0)
        {
            Debug.Log("Undo List Not History's !!");
            return;
        }

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                Pattern_Map[j, i] = UNDO_LIST[UNDO_LIST.Count - 1].Pattern_Map[j, i];
            }
        }

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                Pattern_GO[j, i].gameObject.GetComponent<Pixel>().ChangeColor(UNDO_LIST[UNDO_LIST.Count - 1].Pattern_GO[j, i], false);
            }
        }

        for (int i = 0; i < mapPixel_Y; i++)
        {
            for (int j = 0; j < mapPixel_X; j++)
            {
                if(UNDO_LIST[UNDO_LIST.Count - 1].Pattern_Branch[j, i] == -1)
                {
                    Pattern_GO[j, i].gameObject.GetComponent<Pixel>().isBranch = -1;
                    Pattern_GO[j, i].gameObject.transform.GetChild(0).gameObject.SetActive(false);
                }
                else
                {
                    Pattern_GO[j, i].gameObject.GetComponent<Pixel>().isBranch = UNDO_LIST[UNDO_LIST.Count - 1].Pattern_Branch[j, i];

                    if      (Pattern_GO[j, i].gameObject.GetComponent<Pixel>().isBranch == 4)
                    {
                        Pattern_GO[j, i].gameObject.transform.GetChild(0).gameObject.SetActive(true);
                    }
                    else if (Pattern_GO[j, i].gameObject.GetComponent<Pixel>().isBranch == 3)
                    {
                        Pattern_GO[j, i].gameObject.transform.GetChild(2).gameObject.SetActive(true);
                    }
                }
            }
        }

        for (int i = 0; i < ReadingPatternManager.S.ColorCalcButton.Length; i++)
        {
            ReadingPatternManager.S.ColorCalcWindow[i] = false;
            ReadingPatternManager.S.ColorCalcButton[i].gameObject.transform.GetChild(2).gameObject.SetActive(false);
        }

        ReadingPatternManager.S.ColorCalcWindow[UNDO_LIST[UNDO_LIST.Count - 1].CurrCalcColorIndex] = true;
        ReadingPatternManager.S.ColorCalcButton[UNDO_LIST[UNDO_LIST.Count - 1].CurrCalcColorIndex].gameObject.
            transform.GetChild(2).gameObject.SetActive(true);

        //Debug.Log("Remove Count : " + (UNDO_LIST.Count - 1));
        UNDO_LIST.RemoveAt(UNDO_LIST.Count - 1);
    }

    //ALL DELETE
    public void AllDelete_Active()
    {
        //Debug.Log("All Delete..!");
        GracesGames.SimpleFileBrowser.Scripts.TextFileFinder.S.FileName.text = "";

        ReadingPatternManager.S.InitLogData();
        ReadingPatternManager.S.ResetButtons();

        for (int i = 0; i < ReadingPatternManager.S.MatchDropDown.Length; i++)
        {
            ReadingPatternManager.S.MatchDropDown[i].value = 0;
        }

        switch (currMapSize)
        {
            case CURRENT_MAPSIZE.NONE:
                break;

            case CURRENT_MAPSIZE.P_88:
                Size_88();
                break;

            case CURRENT_MAPSIZE.P_816:
                Size_816();
                break;

            case CURRENT_MAPSIZE.P_168:
                Size_168();
                break;

            case CURRENT_MAPSIZE.P_1616:
                Size_1616();
                break;

            case CURRENT_MAPSIZE.P_1624:
                Size_1624();
                break;

            case CURRENT_MAPSIZE.P_2416:
                Size_2416();
                break;

            case CURRENT_MAPSIZE.P_2424:
                Size_2424();
                break;
        }
    }

    private void Update()
    {
        if(Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("UndoHistory..!");
            UndoHistory();
        }
    }

    //=================================================================
    //CanvaSizeDropDownMenu
    public void ActiveCanvasSizeDropDownMenu(int _canvasSizeIndex)
    {
        ReadingPatternManager.S.InitLogData();
        ReadingPatternManager.S.ResetButtons();
        //Debug.Log("CanvasSizeIndex : " + Canvas_Size_DropDown_Menu.value);

        switch (Canvas_Size_DropDown_Menu.value)
        {
            case 0:
                Size_88();
                break;

            case 1:
                Size_168();
                break;

            case 2:
                Size_816();
                break;

            case 3:
                Size_1616();
                break;

            case 4:
                Size_2416();
                break;

            case 5:
                Size_1624();
                break;

            case 6:
                Size_2424();
                break;
        }
    }

    //CanvaColorDropDownMenu
    public void ActiveCanvasColorDropDownMenu(int _canvasColorIndex)
    {
        //Debug.Log("CanvasColorIndex : " + Canvas_Color_DropDown_Menu.value);
        ActiveMaterial(Canvas_Color_DropDown_Menu.value);
    }
    //=================================================================

    public void ActiveBranchPixel(int _pixelCount)
    {
        if(_pixelCount == 4)
        {
            isPixelBranch_3 = false;
            isPixelBranch_4 = true;

            Color newColor = Branch4Pixel.gameObject.GetComponent<Image>().color;
            newColor.a = 1.0f;

            Branch4Pixel.gameObject.GetComponent<Image>().color = newColor;

            newColor = Branch3Pixel.gameObject.GetComponent<Image>().color;
            newColor.a = 0.5f;

            Branch3Pixel.gameObject.GetComponent<Image>().color = newColor;
        }
        else if(_pixelCount == 3)
        {
            isPixelBranch_4 = false;
            isPixelBranch_3 = true;

            Color newColor = Branch4Pixel.gameObject.GetComponent<Image>().color;
            newColor.a = 0.5f;

            Branch4Pixel.gameObject.GetComponent<Image>().color = newColor;

            newColor = Branch3Pixel.gameObject.GetComponent<Image>().color;
            newColor.a = 1.0f;

            Branch3Pixel.gameObject.GetComponent<Image>().color = newColor;
        }
    }
}
