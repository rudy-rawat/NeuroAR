using UnityEngine;

[System.Serializable]
public class QuizQuestion
{
    [TextArea(2, 4)]
    public string question;           // The question text
    public string[] options;          // 4 options (A, B, C, D)
    public int correctAnswerIndex;    // 0 = A, 1 = B, 2 = C, 3 = D

    public bool IsCorrect(int selectedIndex)
    {
        return selectedIndex == correctAnswerIndex;
    }
}