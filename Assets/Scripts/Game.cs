using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FancyEqualityComparer : IEqualityComparer<int[]>
{
    public bool Equals(int[] x, int[] y)
    {
        //Debug.Log("Checking equality");
        if (x.Length != y.Length)
        {
            return false;
        }
        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i])
            {
                return false;
            }
        }
        return true;
    }

    public int GetHashCode(int[] obj)
    {
        //Debug.Log("Getting Hash Code");
        int result = 17;
        for (int i = 0; i < obj.Length; i++)
        {
            unchecked
            {
                result = result * 23 + obj[i];
            }
        }
        return result;
    }
}

public class Game : MonoBehaviour {

    public static Game instance;
    public static Dictionary<int[], Piece> pieces = new Dictionary<int[], Piece>(new FancyEqualityComparer());
    public static Dictionary<int[], Player> players = new Dictionary<int[], Player>(new FancyEqualityComparer());
    public static List<Player> playersByNum = new List<Player>();
    public static Player localPlayer;

    public static Dictionary<string, Move[]> moveSets = new Dictionary<string, Move[]>();

    public GameObject availableMoveSquare;

    public static Color[] playerColors =
    {
        new Color(0.2f, 0.2f, 1f),
        new Color(0.2f, 1f, 0.2f),
        new Color(1f, 0.2f, 1f),
        new Color(1f, 1f, 0.2f),
        new Color(1f, 0.2f, 0.2f),
        new Color(0.2f, 1f, 1f),
        new Color(0.5f, 1f, 0.1f),
        new Color(0.1f, 1f, 0.5f),
        new Color(1f, 0.5f, 0.1f),
        new Color(0.1f, 0.5f, 1f),
    };

    // Old Shit
    /*
    [System.Serializable]
    public struct Move
    {
        public Move(int lateral, int diagonal = 0, bool onlyForward = false, bool force = false, bool jump = false, bool canConsume = true, bool onlyForConsume = false, bool firstMove = false, int step = 1)
        {
            this.force = force;
            this.jump = jump;
            this.canConsume = canConsume;
            this.onlyForward = onlyForward;
            this.onlyForConsume = onlyForConsume;
            this.lateral = lateral;
            this.diagonal = diagonal;
            this.firstMove = firstMove;
            this.step = step;
        }
        // put 0 for nothing, -1 for as much as you want, if a specific is inputted and force is false, you can move up to that number
        public bool canConsume; // can this move result in the consumption of another piece
        public bool force; // forcing a certain amount (ie knights)
        public bool jump; // can jump over other (ie knights)
        public bool onlyForConsume; // this move can only be used to consume another piece (ie pawns)
        public bool onlyForward; // forward movement (ie pawns) for n-dimensional chess, movement in any component of the direction vector towards the start position of the other colour
        public int lateral; // movement paralel to any coordinate axis (ie rooks, queens, kings)
        public int diagonal; // movement across any 45 degree diagonal (ie bishops, queens)
        public bool firstMove; // can only use this move if it's the first move (pawns)
        public int step; // 1 means nothing gets skipped, 2 means every other step, 3 means every third step...
    }*/

    [System.Serializable]
    public struct Move
    {
        public Move(int dx = 0, int dy = 0, int range = 8, bool jump = false, bool canConsume = true, bool onlyForConsume = false, int step = 1)
        {
            this.range = range;
            this.jump = jump;
            this.canConsume = canConsume;
            this.onlyForConsume = onlyForConsume;
            this.dx = dx;
            this.dy = dy;
            this.step = step;
        }
        
        public bool canConsume; // can this move result in the consumption of another piece
        public bool jump; // can jump over other (ie knights)
        public bool onlyForConsume; // this move can only be used to consume another piece (ie pawns)
        public int dx; // movement in the x direction
        public int dy; // movement in the y direction
        public int step; // 1 means nothing gets skipped, 2 means every other step, 3 means every third step...
        public int range; // maximum amount of steps it can take
    }

    public static int PLAYER_WORTH = 10;

    // Use this for initialization
    void Start () {
        instance = this;

        moveSets.Add("queen", new Move[] {
            new Move(1, 0), // left right
            new Move(1, 1), // diagonals
            new Move(0, 1), // up down
        });
        moveSets.Add("bishop", new Move[] {
            new Move(1, 1) // only diagonals
        });
        moveSets.Add("knight", new Move[] { // only catch is the diagonal needs to have a component vector in the same direction as the lateral (prob needs to be hardcoded)
            new Move(2, 1, 1, true), // 2 left/right and 1 up/down, can only do one step, can jump
            new Move(1, 2, 1, true), // 1 left/right and 2 up/down, can only do one step, can jump
        });
        moveSets.Add("rook", new Move[] {
            new Move(1, 0), // move horizontally
            new Move(0, 1), // move vertically
        });
        moveSets.Add("pawn", new Move[] {
            new Move(1, 0, 1, false, false), // moves horizontally, can't consume
            new Move(0, 1, 1, false, false), // moves vertically, can't consume
            new Move(1, 1, 1, false, true, true), // moves diagonally to consume, can only be used to consume
        });
        moveSets.Add("princess", new Move[] { // combines my two fav pieces from traditional chess (knight and bishop)
            new Move(2, 1, 1, true), // 2 left/right and 1 up/down, can only do one step, can jump
            new Move(1, 2, 1, true), // 1 left/right and 2 up/down, can only do one step, can jump
            new Move(1, 1) // only diagonals
        });
        moveSets.Add("empress", new Move[] {
            new Move(2, 1, 1, true), // 2 left/right and 1 up/down, can only do one step, can jump
            new Move(1, 2, 1, true), // 1 left/right and 2 up/down, can only do one step, can jump
            new Move(1, 0), // move horizontally
            new Move(0, 1), // move vertically
        });

        // Old Shit
        /*
        moveSets.Add("queen", new Move[] {
            new Move(-1),
            new Move(0, -1),
        });
        moveSets.Add("bishop", new Move[] {
            new Move(0, -1) // only diagonals
        });
        moveSets.Add("knight", new Move[] { // only catch is the diagonal needs to have a component vector in the same direction as the lateral (prob needs to be hardcoded)
            new Move(1, 1, false, true, true) // move one space laterally, and one space diagonally, must move 1 forward, can jump over pieces
        });
        moveSets.Add("rook", new Move[] {
            new Move(-1) // move laterally
        });
        moveSets.Add("pawn", new Move[] {
            new Move(1, 0, true, false, false, false), // moves forward, can't consume
            new Move(0, 1, true, false, false, true, true), // moves diagonally to consume, can only be used to consume
        });
        moveSets.Add("princess", new Move[] { // combines my two fav pieces from traditional chess (knight and bishop)
             new Move(1, 1, false, true, true), // knight moves (with jumping)
             new Move(0, -1) // bishop moves (no jumping)
        });
        moveSets.Add("empress", new Move[] {
            new Move(1, 1, false, true, true), // knight moves (with jumping)
            new Move(-1) // rook moves
        });*/
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public static void LerpMove(Transform t, Vector3 pos, float time)
    {
        instance.StartCoroutine(LerpMoveCoroutine(t, pos, time));
    }

    static IEnumerator LerpMoveCoroutine(Transform t, Vector3 pos, float time)
    {
        int m = Mathf.CeilToInt(time / 0.01f);

        for (int i = 0; i < m; i++)
        {
            yield return new WaitForSeconds(Mathf.Min(time-0.01f*i, 0.01f));
            t.position += 2*(pos - t.position) / m;
        }
        t.position = pos;
    }
}
