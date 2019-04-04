using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Checkers
{
    //C++ - TypeDef
    //C# - Renaming
    using ForcedMoves = Dictionary<Piece, List<Vector2Int>>;
    public class Grid : MonoBehaviour
    {
        public GameObject redPiecePrefab, whitePiecePrefab;
        public Vector3 boardOffset = new Vector3(-4.0f, 0.0f, -4.0f);
        public Vector3 pieceOffset = new Vector3(.5f, 0, .5f);
        public Piece[,] pieces = new Piece[8, 8];

        //for drag and drop
        private Vector2Int mouseOver; // Grid coordnates the mouse is over
        private Piece selectedPiece; // Piece that has been clicked & dragged

        private ForcedMoves forcedMoves = new ForcedMoves();

        void Start()
        {
            GenerateBoard();

        }

        // Update is called once per frame

        Vector3 GetWorldPosition(Vector2Int cell)
        {
            return new Vector3(cell.x, 0, cell.y) + boardOffset + pieceOffset;
        }
        void MovePiece(Piece piece, Vector2Int newCell)
        {
            Vector2Int oldCell = piece.cell;
            pieces[oldCell.x, oldCell.y] = null;
            pieces[newCell.x, newCell.y] = piece;
            piece.oldcell = oldCell;
            piece.cell = newCell;
            piece.transform.localPosition = GetWorldPosition(newCell);
        }
        void GeneratePiece(GameObject prefab, Vector2Int desiredCell)
        {
            GameObject clone = Instantiate(prefab, transform);
            Piece piece = clone.GetComponent<Piece>();
            piece.oldcell = desiredCell;
            piece.cell = desiredCell;
            MovePiece(piece, desiredCell);
        }
        void GenerateBoard()
        {
            Vector2Int desiredCell = Vector2Int.zero;
            // Generate White team
            for (int y = 0; y < 3; y++)
            {
                bool oddRow = y % 2 == 0;
                //Loop through columns
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
                //Loop through columns
                for (int x = 0; x < 8; x += 2)
                {
                    desiredCell.x = oddRow ? x : x + 1;
                    desiredCell.y = y;
                    // Generate Piece
                    GeneratePiece(redPiecePrefab, desiredCell);

                }

            }
        }
        //Check if given coordinates are out of the board range
        bool IsOutOfBounds(Vector2Int cell)
        {
            return cell.x < 0 || cell.x >= 8 ||
                   cell.y < 0 || cell.y >= 8;
        }
        //Return a piece at a given cell
        Piece GetPiece(Vector2Int cell)
        {
            return pieces[cell.x, cell.y];
        }
        Piece SelectPiece(Vector2Int cell)
        {
            // Check if X and Y is out of bounds
            if (IsOutOfBounds(cell))
            {
                //Return result early
                return null;
            }
            //Get the piece at X and Y location
            Piece piece = GetPiece(cell);
            //Check that it isn't null
            if (piece)
            {
                return piece;
            }
            return null;
        }
        //Updating when the pieces have been selected
        void MouseOver()
        {
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            //if the ray hit the board
            if(Physics.Raycast(camRay, out hit))
            {
                // Convert mouse coordinates to 2D array coordinates
                mouseOver.x = (int)(hit.point.x - boardOffset.x);
                mouseOver.y = (int)(hit.point.z - boardOffset.z);
            }
            else // otherwise
            {
                // Default to error (-1)
                mouseOver = new Vector2Int(-1, -1);
            }
        }
        //Drags the selected piece using Raycast location
        void DragPiece(Piece selected)
        {
            Ray camRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(camRay, out hit))
            {
                selected.transform.position = hit.point + Vector3.up;
            }
        }
        // Tries moving a piece from Current (x1 + y1) to Desired (x2 + y2) coordinates
        bool TryMove(Piece selected,Vector2Int desiredCell)
        {
            // Get the selected piece's cell
            Vector2Int startCell = selected.cell;
            // Is this NOT a Valid Move?
            if (!ValidMove(selected, desiredCell))
            {
                // Move it back to original
                MovePiece(selected, startCell);
                Debug.Log("Invalid");
                // Not a valid move
                return false;
            }
            //  Replace end coordinates with our selected piece
            MovePiece(selected, desiredCell);
            // Valid move detected!
            return true;
        }
        void Update()
        {
            //Temporarily place here
            DetectForcedMoves();
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
                    TryMove(selectedPiece, mouseOver);
                    // Try moving the selected piece to where the mouse is over
                    selectedPiece = null;
                }
            }
        }
        bool ValidMove(Piece selected, Vector2Int desiredCell)
        {
            Vector2Int direction = selected.cell - desiredCell;

            #region Rule #01 - Is the piece out of bounds?
            if (IsOutOfBounds(desiredCell))
            {
                // Move it back to original
               
                Debug.Log("Invalid");
                // Not a valid move
                return false;
            }
            #endregion
            #region Rule #02 - Is the selected cell the same as desired?
            if(selected.cell == desiredCell)
            {
                Debug.Log("Invalid - Putting pieces back");
                return false;
            }
            #endregion
            #region Rule #03 - Is the piece at the desired cell not empty?
            if (GetPiece(desiredCell))
            {
                Debug.Log("Invalid - You can't go on top");
                return false;
            }
            #endregion
            #region Rule #04 - Is there any forced moves?
            //Is there any forced moves?
            if (HasForcedMoves(selected))
            {
                if (!IsForedMove(selected, desiredCell))
                {
                    Debug.Log("<color=red>Invalid - you have to us forced moves</color>");
                    return false;
                }
            }
            #endregion
            #region Rule #05 - Is the selected cell being dragged two cells over?
            // Is the piece moved two spaces?
            if(direction.magnitude > 2)
            {
                //Is there no forced moves?
                if(forcedMoves.Count == 0)
                {
                    Debug.Log("<color=red>Invalid- you can only move two</color>");
                    return false;
                }
            }
            #endregion
            #region Rule #06 - Is the piece not going in a diagonal cell?
            //Is the player not moving diagonally?
            if(Mathf.Abs(direction.x) != Mathf.Abs(direction.y))
            {
                Debug.Log("<color=yellow>NOOOOOO</color>");
                return false;
            }
            #endregion
            #region Rule #07 - Is the piece moving in the right direction?
            if (!selectedPiece.isKing)
            {
                //Is the selected piece white?
                if (selectedPiece.isWhite)
                {
                    //Is it moving down?
                    if (direction.y > 0)
                    {
                        Debug.Log("<color=red>Can't move white piece</color>");
                        return false;
                    }
                }
                else
                {
                    if (direction.y < 0)
                    {
                        Debug.Log("<color=red>Can't move red piece</color>");
                        return false;
                    }
                }
            }
            #endregion
            Debug.Log("Success");
            return true;
        }
        void CheckForcedMove(Piece piece)
        {
            Vector2Int cell = piece.cell;
            
            for(int x = -1; x<=1; x += 2)
            {
                for(int y = -1; y <=1; y += 2)
                {
                    Vector2Int offset = new Vector2Int(x, y);

                    Vector2Int desiredCell = cell + offset;
                    #region Check #01 - Correct direction?
                    if (!piece.isKing)
                    {
                        // Is the piece white?
                        if (piece.isWhite)
                        {
                            if(desiredCell.y < cell.y)
                            {
                                //Invalid - Check next one
                                continue; 
                            }
                        }
                        // Is the piece red?
                        else
                        {
                            if (desiredCell.y > cell.y)
                            {
                                //Invalid - Check next one
                                continue;
                            }
                        }
                    }
                    #endregion
                    #region Check #02 - Is the adjacent cell out of bounds?
                    //Is desired cell out of bounds?
                    if (IsOutOfBounds(desiredCell))
                    {
                        //Invalid - check next one
                        continue;
                    }
                    #endregion

                    Piece detectedPiece = GetPiece(desiredCell);

                    #region Check #03 - Is the desired cell empty?
                    // Is there a detected piece?
                    if(detectedPiece == null)
                    {
                        //Invalid
                        continue;
                    }
                    #endregion
                    #region Check #04 - Is the detected piece the same color?
                    //Is the detected piece the same colour?
                    if(detectedPiece.isWhite == piece.isWhite)
                    {
                        continue;
                    }

                    #endregion
                    Vector2Int jumpCell = cell + (offset * 2);
                    #region Check #05 - Is the jump cell out of bounds?
                    if (IsOutOfBounds(jumpCell))
                    {
                        continue;
                    }
                    #endregion
                    #region Check #06 - Is there a piece at the jump cell?
                    //Is there a piece there?
                    if (detectedPiece)
                    {
                        continue;
                    }
                    #endregion
                    #region Store Forced Move
                    //Check if forced moves contains the piece we're currently checking
                    if (!forcedMoves.ContainsKey(piece))
                    {
                        //Add it to list of forced moves
                        forcedMoves.Add(piece, new List<Vector2Int>());
                    }
                    //Add thejump cell to the piece's forced moves
                    forcedMoves[piece].Add(jumpCell);
                    #endregion
                }
            }
        }
        void DetectForcedMoves()
        {
            //Refesh forced moves
            forcedMoves = new ForcedMoves();
            //Loop through entire board
            for(int x = 0; x< 8; x++)
            {
                for(int y = 0; y < 8; y++)
                {
                    //Get piece at index
                    Piece pieceToCheck = pieces[x, y];
                    if (pieceToCheck)
                    {
                        //Check piece for forced moves
                        CheckForcedMove(pieceToCheck);
                    }
                }
            }
        }
        // Check if a piece has forced pieces based on color
        bool HasForcedMoves(Piece selected)
        {
            //Loop through all forced moves
            foreach (var move in forcedMoves)
            {
                //Get piece for forced move
                Piece piece = move.Key;
                //Is the piece being forced to move the same color as selected piece
                if (piece.isWhite == selected.isWhite)
                {
                    //Our selected piece has forced move(s)
                    return true;

                }

            }
            // Our selected piece has forced move(s)
            return false;
        }
        // Check if the selected piece has forced moves
        bool IsForedMove(Piece selected, Vector2Int desiredCell)
        {
            // Does the selected piece have a forced move?
            if (forcedMoves.ContainsKey(selected))
            {
                //Is there any forced moves for this piece?
                if (forcedMoves[selected].Contains(desiredCell))
                {
                    // It is a forced move
                    return true;
                }
            }
            // It is not a forced move
            return false;
        }
        //Remove a piece from the board
        void RemovePiece(Piece pieceToRemove)
        {
            Vector2Int cell = pieceToRemove.cell;
            //clear cell in 2d array
            pieces[cell.x, cell.y] = null;
            //Destroy the gameobject of the piece immediately
            DestroyImmediate(pieceToRemove.gameObject);
        }

        Piece GetPieceBetween(Vector2Int start, Vector2Int end)
        {
            Vector2Int cell = Vector2Int.zero;
            cell.x = (start.x + end.x) / 2;
            cell.y = (start.y + end.y) / 2;
            return GetPiece(cell);
        }
        //Tries moving a piece from current(x1+y1) to Desired (x2+y2) coordinates
        bool Trymove(Piece selected, Vector2Int desiredCell)
        {
            //Get the selected piece's cell
            Vector2Int startCell = selected.cell;
            //Is desired cell is out of Bounds;
            if (IsOutOfBounds(desiredCell))
            {
                //Move it back to original
                MovePiece(selected, startCell);
                Debug.Log("<color=red>Invalid - you cannot move outside of the bounds");
            }
        }
    }

}



