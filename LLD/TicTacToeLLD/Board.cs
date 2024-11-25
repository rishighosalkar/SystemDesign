using System;

namespace TicTacToeLLD;

public class Board
{
    public int size;
    public PlayingPiece[,] board;

    public Board(int size)
    {
        this.size = size;
        board = new PlayingPiece[this.size, this.size];
    }

    public bool IsBoardEmpty() {

        for(int i=0; i<size; i++) {
            for(int j=0; j<size; j++) {
                if(board[i, j] == null)
                    return true;
            }
        }

        return false;
    }

    public bool AddPiece(int row, int col, PlayingPiece piece) {
        if(board[row, col] != null)
            return false;
        board[row, col] = piece;
        return true;
    }

    public void DisplayBoard() {
        for(int i=0;i<size; i++) {
            for(int j=0; j<size; j++) {
                if(board[i,j] != null)
                    Console.Write(board[i,j].pieceType.ToString() + " ");
                else
                    Console.Write("  ");
                Console.Write(" | ");
            }
            Console.WriteLine();
        }
    }
}
