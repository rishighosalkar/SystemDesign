using System;

namespace TicTacToeLLD;

public class Game
{
    LinkedList<Player> players = new LinkedList<Player>();
    Board gameBoard;

    public void InitializeGame() {
        PlayingPiece player1 = new PlayingPieceX();
        PlayingPiece player2 = new PlayingPieceO();

        players.AddLast(new Player(){Name="player1",playingPiece = player1});
        players.AddLast(new Player(){Name="player2",playingPiece = player2});

        gameBoard = new Board(3);
    }

    public void StartGame() {

        bool status = true;
        while(status) {
            Player currPlayer = players.First.Value;
            
            if(!gameBoard.IsBoardEmpty()){
                Console.WriteLine("Its a draw");
                status=false;
                continue;
            }
            
            Console.WriteLine("Input row for : " + currPlayer.Name);
            int row = Convert.ToInt32(Console.ReadLine());
            Console.WriteLine("Input col for : " + currPlayer.Name);
            int col = Convert.ToInt32(Console.ReadLine());

            bool pieceAddedSuccessfully = gameBoard.AddPiece(row, col, currPlayer.playingPiece);
            if(!pieceAddedSuccessfully) {
                Console.WriteLine("Please enter correct input: " + currPlayer.Name);
                continue;
            }

            players.RemoveFirst();
            
            if(IsWinner(row, col, currPlayer.playingPiece.pieceType)) {
                Console.WriteLine("Winner is: " + currPlayer.Name);
                status=false;
            }
            gameBoard.DisplayBoard();
            players.AddLast(currPlayer);

        }
    }

    public bool IsWinner(int row, int col, PieceType pieceType) {
        bool rowMatch = true, colMatch = true, diagonalMatch = true, antiDiagonalMatch = true;
        for(int i=0; i<gameBoard.size; i++){
           if(gameBoard.board[row, i] == null || gameBoard.board[row, i].pieceType != pieceType)
                rowMatch = false;
        }

        for(int i=0; i<gameBoard.size; i++){
            if(gameBoard.board[i, col] == null || gameBoard.board[i, col].pieceType != pieceType){
                colMatch = false;
            }
        }

        for(int i=0, j=0; i<gameBoard.size;i++,j++) {
            if (gameBoard.board[i,j] == null || gameBoard.board[i,j].pieceType != pieceType) {
                diagonalMatch = false;
            }
        }

        for(int i=0, j=gameBoard.size-1; i<gameBoard.size;i++,j--) {
            if (gameBoard.board[i,j] == null || gameBoard.board[i,j].pieceType != pieceType) {
                antiDiagonalMatch = false;
            }
        }


        return rowMatch || colMatch || diagonalMatch || antiDiagonalMatch;
    }
}
