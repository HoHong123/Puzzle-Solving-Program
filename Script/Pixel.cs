using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Pixel : MonoBehaviour {

    [Header("My Pos")]
    public int my_X = -1;
    public int my_Y = -1;

    public Renderer myMat = null;

    public int  currCount   = -1;
    public int  isBranch    = -1;

    private void Awake()
    {
        myMat = this.gameObject.GetComponent<Renderer>();
    }

    private void OnMouseEnter()
    {
        if (ReadingPatternManager.S.isOKCheck) return;
        if (EventSystem.current.IsPointerOverGameObject()) return;
        if (MapManager.S.isDrag == false) return;

        // 컨트롤을 누리지 않았으면 실행
        if (Input.GetKey(KeyCode.LeftControl) == false)
        {
            // 좌클릭시 
            if (Input.GetMouseButton(0))
            {
                if (currCount == MapManager.S.SelectToMatIndex) return;

                // 컬러를 색상 리스트에 존재하는 i번째 색상으로 변경
                ChangeColor(MapManager.S.SelectToMatIndex);
            }

            // 우클릭시
            else if (Input.GetMouseButton(1))
            {
                if (currCount == -1) return;

                // 연결 노드 제거
                isBranch = -1;
                this.transform.GetChild(0).gameObject.SetActive(false);
                // 컬러를 제거
                ChangeColor(-1);
            }
        }
        // 컨트롤을 눌렀을 경우
        else
        {
            // 좌클릭시
            if (Input.GetMouseButton(0))
            {
                // 연결된 노드가 없거나
                if (currCount == -1 || isBranch != -1) return;

                // 4픽셀 검색 상태이면 
                if (MapManager.S.isPixelBranch_4)
                {
                    isBranch = 4;

                    this.transform.GetChild(0).gameObject.SetActive(true);
                }
                else if (MapManager.S.isPixelBranch_3)
                {
                    isBranch = 3;

                    this.transform.GetChild(2).gameObject.SetActive(true);
                } 
            }

            // 우클릭시
            else if (Input.GetMouseButton(1))
            {
                if (currCount == -1 || isBranch == -1) return;

                isBranch = -1;
                this.transform.GetChild(0).gameObject.SetActive(false);
                this.transform.GetChild(2).gameObject.SetActive(false);
            }
        }
    }

    private void OnMouseOver()
    {
        if (ReadingPatternManager.S.isOKCheck) return;
        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (Input.GetKey(KeyCode.LeftControl) == false)
        {
            if(Input.GetMouseButtonDown(0))
            {
                if (currCount == MapManager.S.SelectToMatIndex) return;

                if (Input.GetKey(KeyCode.LeftShift))
                {
                    if(MapManager.S.DSL_StartVec == new Vector2(-1, -1))
                    {
                        MapManager.S.DSL_StartVec = new Vector2(my_X, my_Y);
                    }
                    else
                    {
                        MapManager.S.DSL_EndVec = new Vector2(my_X, my_Y);
                        //Debug.Log("도착!");

                        if ((MapManager.S.DSL_StartVec.x == MapManager.S.DSL_EndVec.x) && (MapManager.S.DSL_StartVec.y != MapManager.S.DSL_EndVec.y))
                        {
                            if (MapManager.S.DSL_StartVec.y > MapManager.S.DSL_EndVec.y)
                            {
                                int x = (int)MapManager.S.DSL_StartVec.x;
                                int sIndex = (int)MapManager.S.DSL_EndVec.y + 1;
                                int eIndex = (int)MapManager.S.DSL_StartVec.y;

                                for (int i = sIndex; i < eIndex; i++)
                                {
                                    MapManager.S.Pattern_GO[x, i].gameObject.GetComponent<Pixel>().ChangeColor(MapManager.S.SelectToMatIndex);
                                }
                            }
                            else
                            {
                                int x = (int)MapManager.S.DSL_StartVec.x;
                                int sIndex = (int)MapManager.S.DSL_StartVec.y + 1;
                                int eIndex = (int)MapManager.S.DSL_EndVec.y;

                                for (int i = sIndex; i < eIndex; i++)
                                {
                                    MapManager.S.Pattern_GO[x, i].gameObject.GetComponent<Pixel>().ChangeColor(MapManager.S.SelectToMatIndex);
                                }
                            }

                            MapManager.S.DSL_StartVec = MapManager.S.DSL_EndVec;
                        }
                        else if ((MapManager.S.DSL_StartVec.x != MapManager.S.DSL_EndVec.x) && (MapManager.S.DSL_StartVec.y == MapManager.S.DSL_EndVec.y))
                        {
                            if (MapManager.S.DSL_StartVec.x > MapManager.S.DSL_EndVec.x)
                            {
                                int y = (int)MapManager.S.DSL_StartVec.y;
                                int sIndex = (int)MapManager.S.DSL_EndVec.x + 1;
                                int eIndex = (int)MapManager.S.DSL_StartVec.x;

                                for (int i = sIndex; i < eIndex; i++)
                                {
                                    MapManager.S.Pattern_GO[i, y].gameObject.GetComponent<Pixel>().ChangeColor(MapManager.S.SelectToMatIndex);
                                }
                            }
                            else
                            {
                                int y = (int)MapManager.S.DSL_StartVec.y;
                                int sIndex = (int)MapManager.S.DSL_StartVec.x + 1;
                                int eIndex = (int)MapManager.S.DSL_EndVec.x;

                                for (int i = sIndex; i < eIndex; i++)
                                {
                                    MapManager.S.Pattern_GO[i, y].gameObject.GetComponent<Pixel>().ChangeColor(MapManager.S.SelectToMatIndex);
                                }
                            }

                            MapManager.S.DSL_StartVec = MapManager.S.DSL_EndVec;
                        }
                        else
                        {
                            MapManager.S.DSL_StartVec = new Vector2(-1, -1);
                        }
                    }
                }
                else
                {
                    MapManager.S.DSL_StartVec = new Vector2(my_X, my_Y);
                }
                MapManager.S.AddHistory();

                MapManager.S.isDrag = true;
                ChangeColor(MapManager.S.SelectToMatIndex);
            }

            else if (Input.GetMouseButtonDown(1))
            {
                if (currCount == -1) return;

                MapManager.S.AddHistory();

                isBranch = -1;
                MapManager.S.isDrag = true;
                this.transform.GetChild(0).gameObject.SetActive(false);
                //ReadingPatternManager.S.ChangeCalcColorWindow(-1);
                ChangeColor(-1);
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (currCount == -1 || isBranch != -1) return;

                if (MapManager.S.isPixelBranch_4)
                {
                    MapManager.S.AddHistory();
                    isBranch = 4;

                    this.transform.GetChild(0).gameObject.SetActive(true);
                    MapManager.S.isDrag = true;
                }
                else if (MapManager.S.isPixelBranch_3)
                {
                    MapManager.S.AddHistory();
                    isBranch = 3;

                    this.transform.GetChild(2).gameObject.SetActive(true);
                    MapManager.S.isDrag = true;
                }
            }

            else if (Input.GetMouseButtonDown(1))
            {
                if (currCount == -1 || isBranch == -1) return;

                MapManager.S.AddHistory();

                isBranch = -1;
                this.transform.GetChild(0).gameObject.SetActive(false);
                this.transform.GetChild(2).gameObject.SetActive(false);

                MapManager.S.isDrag = true;
            }
        }
    }

    private void OnMouseUp()
    {
        MapManager.S.isDrag = false;
    }

    /// <summary>
    /// 드래그마다 색상 변경 및 변경 색상 정보 저장
    /// </summary>
    /// <param name="_changeColorIndex">적용될 색상</param>
    /// <param name="isChangeCalcColor"></param>
    public void ChangeColor(int _changeColorIndex, bool isChangeCalcColor = true)
    {
        if (currCount != -1 && currCount != _changeColorIndex)
        {
            ReadingPatternManager.S.ColorWindow[currCount].text = (int.Parse(ReadingPatternManager.S.ColorWindow[currCount].text) - 1).ToString();

            if (int.Parse(ReadingPatternManager.S.ColorWindow[currCount].text) == 0)
            {
                ReadingPatternManager.S.ColorCalcButton[currCount].gameObject.SetActive(false);
            }

            if (_changeColorIndex != -1)
            {
                ReadingPatternManager.S.ColorWindow[_changeColorIndex].text = (int.Parse(ReadingPatternManager.S.ColorWindow[_changeColorIndex].text) + 1).ToString();

                ReadingPatternManager.S.ColorCalcButton[_changeColorIndex].gameObject.SetActive(true);
            }
        }
        else if(currCount == -1 && _changeColorIndex != -1)
        {
            ReadingPatternManager.S.ColorWindow[_changeColorIndex].text = (int.Parse(ReadingPatternManager.S.ColorWindow[_changeColorIndex].text) + 1).ToString();
            ReadingPatternManager.S.ColorCalcButton[_changeColorIndex].gameObject.SetActive(true);
        }

        //Debug.Log("_changeColorIndex : " + _changeColorIndex);
        // 새로 할당될 색상 설정
        currCount       = _changeColorIndex;

        if(currCount != -1)
        {
            myMat.material = MapManager.S.MatchToMaterials[currCount];

            if(isChangeCalcColor)   ReadingPatternManager.S.ChangeCalcColorWindow(currCount);
        }
        else
        {
            myMat.material = MapManager.S.EaserMat;
        }
    }
}
