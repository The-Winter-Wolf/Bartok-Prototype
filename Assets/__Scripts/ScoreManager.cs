using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Перечисление со всеми возможными событиями начисления очков
public enum eScoreEvent {
    draw,
    mine,
    mineGold,
    gameWin,
    gameLoss
}

public class ScoreManager : MonoBehaviour // Управляет подсчетом очков
{
    static private ScoreManager S;

    static public int SCORE_FROM_PREV_ROUND = 0;
    
    

    [Header("Set Dynamically")]
    // Поля для хранения информации о заработанных очках
    public int          chain = 0;
    public int          scoreRun = 0;
    public int          score = 0;
    static public int   HIGH_SCORE = 0;

    void Awake() 
    {
        // Подготовка скрытого объекта-одиночки
        if (S == null) {S = this;} 
        else {Debug.LogError("ERROR: ScoreManager.Awake(): S is already set!");}

        // Проверить рекорд в PlayerPrefs
        if (PlayerPrefs.HasKey("ProspectorHighScore")) {
            HIGH_SCORE = PlayerPrefs.GetInt("ProspectorHighScore");
        }

        // Добавить очки, заработанные в последнем раунде
        score += SCORE_FROM_PREV_ROUND;
        // Сбросить очки последнего раунда
        SCORE_FROM_PREV_ROUND = 0;
    }

    static public void EVENT(eScoreEvent evt) 
    {
        // try-catch не позволит ошибке аварийно завершить программу
        try {S.Event(evt);}
        catch (System.NullReferenceException nre) {
            Debug.LogError("ScoreManager:EVENT() called while S=null.\n" + nre);
        }
    }

    void Event(eScoreEvent evt) 
    {
        switch (evt) {
            // В случае победы, проигрыша и завершении хода выполняются одни и теже действия
            case eScoreEvent.draw:      // Выбор свободной карты
            case eScoreEvent.gameWin:   // Победа в раунде
            case eScoreEvent.gameLoss:  // Проигрыш в раунде
                chain = 0;              // Сбросить цепочку подсчета очков
                score += scoreRun;      // Добавить scoreRun к общему числу очков
                scoreRun = 0;           // Сбросить scoreRun
                break;

            case eScoreEvent.mine:      // Удаление карты из основной раскладки
                chain++;                // Увеличить количество очков в цепочке
                scoreRun += chain;      // Добавить очки за карту
                break;
        }

        switch (evt) {
            // Обрабатывает победу и проигрыш в раунде
            case eScoreEvent.gameWin:
                // В случае победы перенести очки в следующий раунд
                // Cтатические поля не сбрасываются вызовом SceneManager.LoadScene()
                SCORE_FROM_PREV_ROUND = score;
                print ("You won this round! Round score: " + score);
                break;

            case eScoreEvent.gameLoss:
                // В случае проигрыша сравнить с рекордом
                if (HIGH_SCORE <= score) {
                    print ("You got the high score! High score: " + score);
                    HIGH_SCORE = score;
                    PlayerPrefs.SetInt("ProspectorHighScore", score);
                } else {
                    print ("Your final score for the game was: " + score);
                }
                break;

            default:
                print ("score: " + score + " scoreRun: " + scoreRun + " chain: " + chain);
                break;
        }
    }

    static public int CHAIN { get {return S.chain;} }
    static public int SCORE { get {return S.score;} }
    static public int SCORE_RUN { get {return S.scoreRun;} }  
}
