using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CheckersMkII
{
  // C++ - TypeDef
  // C# - Renaming
  using ForcedMoves = Dictionary<Piece, List<Vector2Int>>;

  public class Grid : MonoBehaviour
  {
    public GameObject redPiecePrefab, whitePiecePrefab;
    public Vector3 boardOffset = new Vector3(-4.0f, 0.0f, -4.0f);
    public Vector3 pieceOffset = new Vector3(.5f, 0, .5f);
    public Piece[,] pieces = new Piece[8, 8];
    public bool isWhiteTurn = true;

    // For Drag and Drop
    private Vector2Int mouseOver; // Grid coordinates the mouse is over
    private Piece selectedPiece; // Piece that has been clicked & dragged

    private ForcedMoves forcedMoves = new ForcedMoves();

    // Converts array coordinates to world position
    Vector3 GetWorldPosition(Vector2Int cell)
    {
      return new Vector3(cell.x, 0, cell.y) + boardOffset + pieceOffset;
    }

    // Moves a Piece to another coordinate on a 2D grid
    void MovePiece(Piece piece, Vector2Int newCell)
    {
      Vector2Int oldCell = piece.cell;
      // Update array
      pieces[oldCell.x, oldCell.y] = null;
      pieces[newCell.x, newCell.y] = piece;
      // Update data on piece
      piece.oldCell = oldCell;
      piece.cell = newCell;
      // Translate the piece to another location
      piece.transform.localPosition = GetWorldPosition(newCell);
    }

    // Generates a Checker Piece in specified coordinates
    void GeneratePiece(GameObject prefab, Vector2Int desiredCell)
    {
      // Generate Instance of prefab
      GameObject clone = Instantiate(prefab, transform);
      // Get the Piece component
      Piece piece = clone.GetComponent<Piece>();
      // Set the cell data for the first time
      piece.oldCell = desiredCell;
      piece.cell = desiredCell;
      // Reposition clone
      MovePiece(piece, desiredCell);
    }

    void GenerateBoard()
    {
      Vector2Int desiredCell = Vector2Int.zero;
      // Generate White Team
      for (int y = 0; y < 3; y++)
      {
        bool oddRow = y % 2 == 0;
        // Loop through columns
        for (int x = 0; x < 8; x += 2)
        {
          desiredCell.x = oddRow ? x : x + 1;
          desiredCell.y = y;
          // Generate Piece
          GeneratePiece(whitePiecePrefab, desiredCell);
        }
      }
      // Generate Red Team
      for (int y = 5; y < 8; y++)
      {
        bool oddRow = y % 2 == 0;
        // Loop through columns
        for (int x = 0; x < 8; x += 2)
        {
          desiredCell.x = oddRow ? x : x + 1;
          desiredCell.y = y;
          // Generate Piece
          GeneratePiece(redPiecePrefab, desiredCell);
        }
      }
    }

    // Use this for initialization
    void Start()
    {
      GenerateBoard();
      StartTurn(); // ← PART 3
    }

    // Checks if given coordinates are out of the board range
    bool IsOutOfBounds(Vector2Int cell)
    {
      return cell.x < 0 || cell.x >= 8 ||
             cell.y < 0 || cell.y >= 8;
    }

    // Returns a piece at a given cell
    Piece GetPiece(Vector2Int cell)
    {
      return pieces[cell.x, cell.y];
    }

    // Selects a piece on the 2D grid and returns it
    Piece SelectPiece(Vector2Int cell)
    {
      // Check if X and Y is out of bounds
      if (IsOutOfBounds(cell))
      {
        // Return result early
        return null;
      }

      // Get the piece at X and Y location
      Piece piece = GetPiece(cell);

      // Check that it is't null
      if (piece && piece.isWhite == isWhiteTurn)
      {
        return piece;
      }

      return null;
    }

    // Updating when the pieces have been selected
    void MouseOver()
    {
      // Perform Raycast from mouse position
      Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
      RaycastHit hit;
      // If the ray hit the board
      if (Physics.Raycast(camRay, out hit))
      {
        // Convert mouse coordinates to 2D array coordinates
        mouseOver.x = (int)(hit.point.x - boardOffset.x);
        mouseOver.y = (int)(hit.point.z - boardOffset.z);
      }
      else // Otherwise
      {
        // Default to error (-1)
        mouseOver = new Vector2Int(-1, -1);
      }
    }

    // Drags the selected piece using Raycast location
    void DragPiece(Piece selected)
    {
      Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
      RaycastHit hit;
      // Detects mouse ray hit point
      if (Physics.Raycast(camRay, out hit))
      {
        // Updates position of selected piece to hit point + offset
        selected.transform.position = hit.point + Vector3.up;
      }
    }

    // Tries moving a piece from Current (x1 + y1) to Desired (x2 + y2) coordinates
    bool TryMove(Piece selected, Vector2Int desiredCell)
    {
      // Get the selected piece's cell
      Vector2Int startCell = selected.cell;
      // Is this NOT a Valid Move?
      if (!ValidMove(selected, desiredCell))
      {
        // Move it back to original
        MovePiece(selected, startCell);
        // Not a valid move
        return false;
      }
      // Replace end coordinates with our selected piece
      MovePiece(selected, desiredCell);
      // Valid move detected!
      return true;
    }

    void Update()
    {
      // Update the mouse over information
      MouseOver();
      // If the mouse is pressed
      if (Input.GetMouseButtonDown(0))
      {
        // Try selecting piece
        selectedPiece = SelectPiece(mouseOver);
      }
      // If there is a selected piece
      if (selectedPiece)
      {
        // Move the piece with Mouse
        DragPiece(selectedPiece);
        // If button is released
        if (Input.GetMouseButtonUp(0))
        {
          // Try moving the selected piece to where the mouse is over
          if (TryMove(selectedPiece, mouseOver))
          {
            // Check if piece was taken
            if (IsPieceTaken(selectedPiece))
            {
              // Add score
              // Update forced moves
              DetectForcedMoves();
            }
            // Check for king (only if the move was successful)
            CheckForKing(selectedPiece);
            // If selected piece doesn't have anymore forced moves
            if (!HasForcedMoves(selectedPiece))
            {
              SwitchTurns();
            }
          }

          // Let go of the piece
          selectedPiece = null;
        }
      }
    }

    void SwitchTurns()
    {
      EndTurn();
      isWhiteTurn = !isWhiteTurn;
      StartTurn();
    }

    bool IsForcedMove(Piece selected, Vector2Int desiredCell)
    {
      // Does the selected piece have a forced move?
      if (forcedMoves.ContainsKey(selected))
      {
        // Is there any forced moves for this piece?
        if (forcedMoves[selected].Contains(desiredCell))
        {
          // It is a forced move
          return true;
        }
      }
      // It is not a forced move
      return false;
    }

    bool HasForcedMoves(Piece selected)
    {
      foreach (var move in forcedMoves)
      {
        Piece piece = move.Key;
        if (piece.isWhite == selected.isWhite)
        {
          // Has forced moves!
          return true;
        }
      }
      // Does not have any forced moves
      return false;
    }

    // Checks if the start and end drag is a valid move
    bool ValidMove(Piece selected, Vector2Int desiredCell)
    {
      // Get direction of movement for some of the next few rules
      Vector2Int direction = selected.cell - desiredCell;

      #region Rule #01 - Is the piece out of bounds?
      // Is desired cell is Out of Bounds?
      if (IsOutOfBounds(desiredCell))
      {
        Debug.Log("<color=red>Invalid - You cannot move out side of the map</color>");
        return false;
      }
      #endregion

      #region Rule #02 - Is the selected cell the same as desired?
      if (selected.cell == desiredCell)
      {
        Debug.Log("<color=red>Invalid - Putting pieces back don't count as a valid move</color>");
        return false;
      }
      #endregion

      #region Rule #03 - Is the piece at the desired cell not empty?
      if (GetPiece(desiredCell))
      {
        Debug.Log("<color=red>Invalid - You can't go on top of other pieces</color>");
        return false;
      }
      #endregion

      #region Rule #04 - Is there any forced moves?
      if (HasForcedMoves(selected))
      {
        // If it is not a forced move
        if (!IsForcedMove(selected, desiredCell))
        {
          Debug.Log("<color=red>Invalid - You have to use forced moves!</color>");
          return false;
        }
      }
      #endregion

      #region Rule #05 - Is the selected cell being dragged two cells over?
      // Is the piece moved two spaces?
      if (direction.magnitude > 2)
      {
        // Is there no forced moves?
        if (forcedMoves.Count == 0)
        {
          Debug.Log("<color=red>Invalid - You can only move two spaces if there are forced moves on selected piece</color>");
          return false;
        }
      }
      #endregion

      #region Rule #06 - Is the piece not going in a diagonal cell?
      // Is the player not moving diagonally?
      if (Mathf.Abs(direction.x) != Mathf.Abs(direction.y))
      {
        Debug.Log("<color=red>Invalid - You have to be moving diagonally</color>");
        return false;
      }
      #endregion

      #region Rule #07 - Is the piece moving in the right direction?
      // Is the selected piece not a king?
      if (!selectedPiece.isKing)
      {
        // Is the selected piece white?
        if (selectedPiece.isWhite)
        {
          // Is it moving down?
          if (direction.y > 0)
          {
            Debug.Log("<color=red>Invalid - Can't move a white piece backwards</color>");
            return false;
          }
        }
        // Is the selected piece red?
        else
        {
          // Is it moving up?
          if (direction.y < 0)
          {
            Debug.Log("<color=red>Invalid - Can't move a red piece backwards</color>");
            return false;
          }
        }
      }
      #endregion

      // If all the above rules haven't returned false, it must be a successful move!
      Debug.Log("<color=green>Success - Valid move detected!</color>");
      return true;
    }

    // Calculates & returns the piece between start and end locations
    Piece GetPieceBetween(Vector2Int start, Vector2Int end)
    {
      Vector2Int cell = Vector2Int.zero;
      cell.x = (start.x + end.x) / 2;
      cell.y = (start.y + end.y) / 2;
      return GetPiece(cell);
    }

    // Removes a piece from the board
    void RemovePiece(Piece pieceToRemove)
    {
      Vector2Int cell = pieceToRemove.cell;
      // Clear cell in 2D array
      pieces[cell.x, cell.y] = null;
      // Destroy the gameobject of the piece immediately
      DestroyImmediate(pieceToRemove.gameObject);
    }

    void EndTurn()
    {

    }

    void OnDrawGizmos()
    {
      // Draw where the tile where the mouse is hovered over
      Vector3 worldPos = transform.position + GetWorldPosition(mouseOver);
      Gizmos.color = Color.red;
      Gizmos.DrawSphere(worldPos, .1f);

      Gizmos.color = Color.blue;
      // Loop through all forced moves
      foreach (var point in forcedMoves)
      {
        Piece piece = point.Key;
        Vector3 piecePos = piece.transform.position;
        // Loop through each cell that is forced
        foreach (var cell in point.Value)
        {
          // Draw World Cell position
          Vector3 cellPos = transform.position + GetWorldPosition(cell);
          Gizmos.DrawSphere(cellPos, .25f);
        }
      }
    }

    // Called once at the start of turn
    void StartTurn()
    {
      DetectForcedMoves();
    }

    // Check if a piece needs to be kinged
    void CheckForKing(Piece selected)
    {
      // Check if the selected piece is not kinged
      if (selected && !selected.isKing)
      {
        bool whiteNeedsKing = selected.isWhite && selected.cell.y == 7;
        bool redNeedsKing = !selected.isWhite && selected.cell.y == 0;
        // If the selected piece is white and reached the end of the board
        if (whiteNeedsKing || redNeedsKing)
        {
          // The selected piece is kinged!
          selected.King();
        }
      }
    }

    // Check if piece was taken
    bool IsPieceTaken(Piece selected)
    {
      // Get the piece in between move
      Piece pieceBetween = GetPieceBetween(selected.oldCell, selected.cell);
      // If there is a piece between and the piece isn't the same color
      if (pieceBetween != null && pieceBetween.isWhite != selected.isWhite)
      {
        // Destroy the piece between
        RemovePiece(pieceBetween);
        // Piece taken
        return true;
      }
      // Piece not taken
      return false;
    }

    // Detect if there is a forced move for a given piece
    void CheckForcedMove(Piece piece)
    {
      // Get cell location of piece
      Vector2Int cell = piece.cell;

      // Loop through adjacent cells of cell
      for (int x = -1; x <= 1; x += 2)
      {
        for (int y = -1; y <= 1; y += 2)
        {
          // Create offset cell from index
          Vector2Int offset = new Vector2Int(x, y);
          // Creating a new X from piece coordinates using offset
          Vector2Int desiredCell = cell + offset;

          #region Check #01 - Correct Direction?
          // Is the piece not king?
          if (!piece.isKing)
          {
            // Is the piece white?
            if (piece.isWhite)
            {
              // Is the piece moving backwards?
              if (desiredCell.y < cell.y)
              {
                // Invalid - Check next one
                continue;
              }
            }
            // Is the piece red?
            else
            {
              // Is the piece moving backwards?
              if (desiredCell.y > cell.y)
              {
                // Invalid - Check next one
                continue;
              }
            }
          }
          #endregion

          #region Check #02 - Is the adjacent cell out of bounds?
          // Is desired cell out of bounds?
          if (IsOutOfBounds(desiredCell))
          {
            // Invalid - Check next one
            continue;
          }
          #endregion

          // Try getting the piece at coordinates
          Piece detectedPiece = GetPiece(desiredCell);

          #region Check #03 - Is the desired cell empty?
          // Is there a detected piece?
          if (detectedPiece == null)
          {
            // Invalid - Check next one
            continue;
          }
          #endregion

          #region Check #04 - Is the detected piece the same color?
          // Is the detected piece the same color
          if (detectedPiece.isWhite == piece.isWhite)
          {
            // Invalid - Check the next one
            continue;
          }
          #endregion

          // Try getting the diagonal cell next to detected piece
          Vector2Int jumpCell = cell + (offset * 2);

          #region Check #05 - Is the jump cell out of bounds?
          // Is the destination cell out of bounds?
          if (IsOutOfBounds(jumpCell))
          {
            // Invalid - Check the next one
            continue;
          }
          #endregion

          #region Check #06 - Is there a piece at the jump cell?
          // Get piece next to the one we want to jump
          detectedPiece = GetPiece(jumpCell);
          // Is there a piece there?
          if (detectedPiece)
          {
            // Invalid - Check the next one
            continue;
          }
          #endregion

          // If you made it here, a forced move has been detected!

          #region Store Forced Move
          // Check if forced moves contains the piece we're currently checking
          if (!forcedMoves.ContainsKey(piece))
          {
            // Add it to list of forced moves
            forcedMoves.Add(piece, new List<Vector2Int>());
          }
          // Add the jump cell to the piece's forced moves
          forcedMoves[piece].Add(jumpCell);
          #endregion
        }
      }
    }

    // Scans the board for forced moves
    void DetectForcedMoves()
    {
      // Refresh forced moves
      forcedMoves = new ForcedMoves();
      // Loop through entire board
      for (int x = 0; x < 8; x++)
      {
        for (int y = 0; y < 8; y++)
        {
          // Get piece at index
          Piece pieceToCheck = pieces[x, y];
          // If the piece exists
          if (pieceToCheck)
          {
            // Check piece for forced moves
            CheckForcedMove(pieceToCheck);
          }
        }
      }
    }
  }
}



