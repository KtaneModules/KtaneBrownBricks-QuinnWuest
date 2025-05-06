using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public class BrownBricks : MonoBehaviour
{
    public KMBombModule Module;
    public KMAudio Audio;
    public KMSelectable[] BrickSels;
    public MeshRenderer[] BrickRenderers;
    public Texture[] BrickTextures;

    private static readonly BlockType[][][] _answerLookup = new BlockType[][][]
    {
        new BlockType[][]
        {
            new BlockType[] {BlockType.Hit, BlockType.Brick},
            new BlockType[] {BlockType.Wall, BlockType.Question, BlockType.Ground, BlockType.Wall}
        },
        new BlockType[][]
        {
            new BlockType[] {BlockType.Brick, BlockType.Ground},
            new BlockType[] {BlockType.Hit, BlockType.Wall, BlockType.Question, BlockType.Ground}
        },
        new BlockType[][]
        {
            new BlockType[] {BlockType.Wall, BlockType.Hit},
            new BlockType[] {BlockType.Ground, BlockType.Brick, BlockType.Ground, BlockType.Question}
        },
        new BlockType[][]
        {
            new BlockType[] {BlockType.Question, BlockType.Wall},
            new BlockType[] {BlockType.Ground, BlockType.Brick, BlockType.Wall, BlockType.Hit}
        },
        new BlockType[][]
        {
            new BlockType[] {BlockType.Ground, BlockType.Question},
            new BlockType[] {BlockType.Question, BlockType.Brick, BlockType.Hit, BlockType.Wall}
        },
    };

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private int _answerBrickPos;
    private BlockType[] _brickPositions;

    enum BlockType
    {
        Hit,
        Question,
        Wall,
        Ground,
        Brick
    }

    private readonly BlockType[] _missingBrickIxs = new BlockType[] { BlockType.Hit, BlockType.Ground, BlockType.Wall, BlockType.Question, BlockType.Wall };
    private readonly BlockType[] _lookBrickIxs = new BlockType[] { BlockType.Wall, BlockType.Ground, BlockType.Brick, BlockType.Hit, BlockType.Question };

    private static readonly string[] _positionNames = new string[] { "top-left", "top-right", "bottom-left", "bottom-right" };

    void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < BrickSels.Length; i++)
            BrickSels[i].OnInteract += PressBrick(i);
        GenerateBricks();
    }

    void GenerateBricks()
    {
        _brickPositions = Enumerable.Range(0, 5).Select(i => (BlockType)i).ToArray().Shuffle();
        for (int i = 0; i < 4; i++)
            BrickRenderers[i].material.mainTexture = BrickTextures[(int)_brickPositions[i]];

        var missingBrick = _brickPositions[4];
        var positionToLookAt = (int)_missingBrickIxs[(int)missingBrick];
        var lookBrick = _lookBrickIxs[(int)_brickPositions[positionToLookAt]];
        var lookBrickPos = Array.IndexOf(_brickPositions, lookBrick);
        BlockType answerBrick;

        if (missingBrick == _answerLookup[lookBrickPos][0][0])
            answerBrick = _answerLookup[lookBrickPos][1][0];

        else if (lookBrick == _answerLookup[lookBrickPos][0][1])
            answerBrick = _answerLookup[lookBrickPos][1][1];
        else
            answerBrick = _answerLookup[lookBrickPos][1][2];

        if (missingBrick == answerBrick)
            answerBrick = _answerLookup[lookBrickPos][1][3];

        _answerBrickPos = Array.IndexOf(_brickPositions, answerBrick);

        Debug.LogFormat("[Brown Bricks #{0}] The blocks in order are {1}, and {2}.", _moduleId, Enumerable.Range(0, 3).Select(i => _brickPositions[i]).Join(", "), _brickPositions[3]);
        Debug.LogFormat("[Brown Bricks #{0}] The missing block is {1}.", _moduleId, missingBrick);
        Debug.LogFormat("[Brown Bricks #{0}] The block to identify is at the {1} position.", _moduleId, _positionNames[positionToLookAt]);
        Debug.LogFormat("[Brown Bricks #{0}] The >LOOK> block that has been transcribed to is {1}.", _moduleId, lookBrick);
        Debug.LogFormat("[Brown Bricks #{0}] The correct block to press is {1}.", _moduleId, answerBrick);
    }

    KMSelectable.OnInteractHandler PressBrick(int brick)
    {
        return delegate ()
        {
            if (_moduleSolved)
                return false;
            Audio.PlaySoundAtTransform("breakblock", transform);
            BrickSels[brick].AddInteractionPunch(.5f);
            if (brick == _answerBrickPos)
            {
                Debug.LogFormat("[Brown Bricks #{0}] Pressing {1} was correct. Module solved.", _moduleId, _brickPositions[brick]);
                _moduleSolved = true;
                Module.HandlePass();
            }
            else
            {
                Debug.LogFormat("[Brown Bricks #{0}] Pressing {1} was incorrect. Strike!", _moduleId, _brickPositions[brick]);
                Module.HandleStrike();
                GenerateBricks();
            }
            return false;
        };
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} press 'tl/tr/bl/br' to press a block. e.g. '!{0} press tr'";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        var m = Regex.Match(command, @"^\s*press\s+(?<button>tl|tr|bl|br)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        if (!m.Success)
            yield break;
        yield return null;
        string[] validCommands = new string[4] { "tl", "tr", "bl", "br" };
        int ix = Array.IndexOf(validCommands, m.Groups["button"].Value);
        BrickSels[ix].OnInteract();
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        BrickSels[_answerBrickPos].OnInteract();
    }
}
