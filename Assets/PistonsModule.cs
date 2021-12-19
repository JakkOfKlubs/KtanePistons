using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;
using System.Text.RegularExpressions;

public class PistonsModule : MonoBehaviour
{
    public KMBombInfo BombInfo;
    public KMAudio Audio;
    public KMBombModule Module;

    public KMSelectable[] squareSelectables;
    public GameObject[] squareSelectableObjs;

    public GameObject[] pistonHeads;
    public GameObject[] pistonsInnerSide;
    public GameObject leverSwitch;

    public GameObject wool;
    public Material[] woolMats;

    public KMSelectable cwButton;
    public KMSelectable ccwButton;
    public GameObject pistonOption;
    private float pistonRotNum = 0;

    public KMSelectable[] buildOptions;
    public GameObject[] buildOptionsObj;
    private int buildOption = 0;

    public KMSelectable buildToggle;
    public GameObject buildToggleObj;
    private Coroutine moveBuildToggle;
    private bool buildingMode = true;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    private Coroutine extendPiston;
    private Coroutine flipLever;
    private Coroutine rotatePiston;
    private bool isExtended;
    private bool leverDown;

    private bool[] occupiedSquares = new bool[36];

    public PistonPrefabObj PistonPrefab;
    public RedstonePrefabObj RedstonePrefab;
    public LeverPrefabObj LeverPrefab;

    public Mesh leverHighlight;

    private PistonPrefabObj piston;
    private PistonPrefabObj[] pistonObjs = new PistonPrefabObj[36];
    private RedstonePrefabObj redstone;
    private RedstonePrefabObj[] redstoneObjs = new RedstonePrefabObj[36];
    private LeverPrefabObj lever;
    private LeverPrefabObj[] leverObjs = new LeverPrefabObj[36];
    private bool[] pistonSquares = new bool[36];
    private bool[] redstoneSquares = new bool[36];
    private bool[] leverSquares = new bool[36];
    private bool[] leverStates = new bool[36];
    private bool _isToggling;

    private float[] xPos = { -0.065f, -0.045f, -0.025f, -0.005f, 0.015f, 0.035f };
    private float[] zPos = { 0.035f, 0.015f, -0.005f, -0.025f, -0.045f, -0.065f };

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < squareSelectables.Length; i++)
        {
            int j = i;
            squareSelectables[j].OnInteract += delegate ()
            {
                if (buildingMode)
                {
                    if (!occupiedSquares[j])
                    {
                        if (buildOption == 0)
                        {
                            pistonObjs[j] = Instantiate(PistonPrefab, transform);
                            pistonObjs[j].transform.localPosition = new Vector3(xPos[j % 6], 0.02f, zPos[j / 6]);
                            pistonObjs[j].transform.localEulerAngles = new Vector3(0f, 180 + pistonRotNum, 90f);
                            pistonSquares[j] = false;
                            Audio.PlaySoundAtTransform("PistonPlace", transform);
                        }
                        else if (buildOption == 1)
                        {
                            redstoneObjs[j] = Instantiate(RedstonePrefab, transform);
                            redstoneObjs[j].transform.localPosition = new Vector3(xPos[j % 6], 0.012f, zPos[j / 6]);
                            redstoneSquares[j] = false;
                            Audio.PlaySoundAtTransform("PistonPlace", transform);
                        }
                        else
                        {
                            leverObjs[j] = Instantiate(LeverPrefab, transform);
                            leverObjs[j].transform.localPosition = new Vector3(xPos[j % 6], 0.015f, zPos[j / 6]);
                            leverSquares[j] = true;
                            Audio.PlaySoundAtTransform("LeverPlace", transform);
                        }
                        occupiedSquares[j] = true;
                    }
                    else
                    {
                        if (pistonObjs[j] != null)
                        {
                            Destroy(pistonObjs[j].gameObject);
                            pistonSquares[j] = false;
                            Audio.PlaySoundAtTransform("PistonBreak", transform);
                        }
                        if (redstoneObjs[j] != null)
                        {
                            Destroy(redstoneObjs[j].gameObject);
                            redstoneSquares[j] = false;
                            Audio.PlaySoundAtTransform("PistonBreak", transform);
                        }
                        if (leverObjs[j] != null)
                        {
                            Destroy(leverObjs[j].gameObject);
                            leverSquares[j] = false;
                            Audio.PlaySoundAtTransform("LeverBreak", transform);
                        }
                        occupiedSquares[j] = false;
                    }
                    CheckRedstone();
                }
                else
                {
                    if (leverSquares[j] == true)
                    {
                        if (leverStates[j] == false) {
                            flipLever = StartCoroutine(FlipLever(j, -0.01f, 0.01f, 45f, -45f));
                            leverStates[j] = true;
                            Audio.PlaySoundAtTransform("LeverFlickOn", transform);
                        }
                        else
                        {
                            flipLever = StartCoroutine(FlipLever(j, 0.01f, -0.01f, -45f, 45f));
                            leverStates[j] = false;
                            Audio.PlaySoundAtTransform("LeverFlickOff", transform);
                        }
                    }
                }
                return false;
            };
        }
        for (int i = 0; i < buildOptions.Length; i++)
        {
            int j = i;
            buildOptions[j].OnInteract += delegate ()
            {
                SelectBuildOption(j);
                return false;
            };
        }

        cwButton.OnInteract += delegate ()
        {
            rotatePiston = StartCoroutine(RotatePiston(pistonRotNum, pistonRotNum + 90));
            pistonRotNum += 90;
            return false;
        };
        ccwButton.OnInteract += delegate ()
        {
            rotatePiston = StartCoroutine(RotatePiston(pistonRotNum, pistonRotNum - 90));
            pistonRotNum -= 90;
            return false;
        };
        buildToggle.OnInteract += delegate ()
        {
            if (!_isToggling)
            {
                if (buildingMode)
                {
                    moveBuildToggle = StartCoroutine(MoveBuildToggle(-0.0315f, -0.0035f));
                    buildingMode = false;
                    for (int i = 0; i < squareSelectableObjs.Length; i++)
                    {
                        squareSelectables[i].Highlight.transform.localScale = new Vector3(2.1f, 1f, 2.9f);
                        squareSelectables[i].GetComponent<BoxCollider>().size = new Vector3(0.45f, 0.55f, 0.005f);
                        squareSelectableObjs[i].SetActive(false);
                    }
                    for (int i = 0; i < 36; i++)
                    {
                        if (leverSquares[i] == true)
                        {
                            squareSelectableObjs[i].SetActive(true);
                        }
                    }
                }
                else
                {
                    moveBuildToggle = StartCoroutine(MoveBuildToggle(-0.0035f, -0.0315f));
                    buildingMode = true;
                    for (int i = 0; i < squareSelectableObjs.Length; i++)
                    {
                        squareSelectableObjs[i].SetActive(true);
                        squareSelectables[i].Highlight.transform.localScale = new Vector3(5f, 1f, 5f);
                        squareSelectables[i].GetComponent<BoxCollider>().size = new Vector3(1f, 1f, 0.005f);
                    }
                }
            }
            return false;
        };


        WoolInfo();
    }

    void WoolInfo()
    {
        int randColor = Rnd.Range(0, 15);
        wool.GetComponent<MeshRenderer>().material = woolMats[randColor];
    pickColor:
        int randPos = Rnd.Range(0, 36);
        if (randPos % 6 == 0 || randPos % 6 == 5 || randPos / 6 == 0 || randPos / 6 == 5)
            goto pickColor;
        wool.transform.localPosition = new Vector3(xPos[randPos % 6], 0.02f, zPos[randPos / 6]);
        occupiedSquares[randPos] = true;
    }

    void SelectBuildOption(int option)
    {
        for (int i = 0; i < 3; i++)
        {
            if (i == option)
            {
                buildOptionsObj[i].SetActive(true);
                buildOption = option;
            }
            else
                buildOptionsObj[i].SetActive(false);
        }
    }

    List<int> GetAdjacents(int square)
    {
        List<int> adjacents = new List<int>();
        if (square % 6 != 0)
            adjacents.Add(square - 1);
        if (square % 6 != 5)
            adjacents.Add(square + 1);
        if (square / 6 != 0)
            adjacents.Add(square - 6);
        if (square / 6 != 5)
            adjacents.Add(square + 6);
        return adjacents;
    }

    void CheckRedstone()
    {

    }

    IEnumerator ExtendPiston(float a, float b, bool innerActive, int pistonNum)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            pistonHeads[0].transform.localPosition = new Vector3(0f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            if ((elapsed > 0.06f && !isExtended) || (elapsed > 0.04f && isExtended))
                pistonsInnerSide[pistonNum].SetActive(innerActive);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }
    
    IEnumerator FlipLever(int lever, float a, float b, float x, float y)
    {
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            leverObjs[lever].leverSwitch.transform.localEulerAngles = new Vector3(Easing.InOutQuad(elapsed, x, y, duration), 0f, 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    IEnumerator RotatePiston(float a, float b)
    {
        var duration = 0.2f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            pistonOption.transform.localEulerAngles = new Vector3(90f, Easing.InOutQuad(elapsed, a, b, duration), 0f);
            yield return null;
            elapsed += Time.deltaTime;
        }
    }

    IEnumerator MoveBuildToggle(float a, float b)
    {
        _isToggling = true;
        var duration = 0.2f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            buildToggle.transform.localPosition = new Vector3(0.065f, 0.0155f, Easing.InOutQuad(elapsed, a, b, duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        _isToggling = false;
    }
}