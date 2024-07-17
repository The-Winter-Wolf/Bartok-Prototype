using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// CBState включает состояния игры и состояния to..., описывающие движения
public enum CBState 
{
    toDrawpile,
    drawpile,
    toHand,
    hand,
    toTarget,
    target,
    discard,
    to,
    idle
}

public class CardBartok : Card
{
    // Статические переменные совместно используются всеми экземплярами CardBartok
    static public float         MOVE_DURATION = 0.5f;
    static public string        MOVE_EASING = Easing.InOut;
    static public float         CARD_HEIGHT = 3.5f;
    static public float         CARD_WIDTH = 2f;

    [Header("Set Dynamically: CardBartok")]
    public CBState              state = CBState.drawpile;

    // Поля с информацией, необходимой для перемещения и поворачивания карты
    public List<Vector3>        bezierPts;
    public List<Quaternion>     bezierRots;
    public float                timeStart, timeDuration;
    public int                  eventualSortOrder;
    public string               eventualSortLayer;

    // По завершении перемещения карты будет вызываться reportFinishTo.SendMessage()
    public GameObject           reportFinishTo = null;
    [System.NonSerialized]
    public Player               callbackPlayer = null;

    // MoveTo запускает перемещение карты в новое местоположение с заданным поворотом
    public void MoveTo(Vector3 ePos, Quaternion eRot)
    {
        // Создать новые списки для интерполяции.
        // Траектории перемещения и поворота определяются двумя точками каждая.
        bezierPts = new List<Vector3>();
        bezierPts.Add (transform.localPosition);    // Текущее местоположение
        bezierPts.Add (ePos);                       // Новое местоположение

        bezierRots = new List<Quaternion>();
        bezierRots.Add (transform.rotation);        // Текущий угол поворота
        bezierRots.Add (eRot);                      // Новый угол поворота

        if (timeStart == 0) {timeStart = Time.time;}
        timeDuration = MOVE_DURATION;
        state = CBState.to;
    }

    public void MoveTo(Vector3 ePos)
    {
        MoveTo(ePos, Quaternion.identity);
    }

    void Update()
    {

        // Выйти из приложения
        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }

        switch (state) {
            case CBState.toHand:
            case CBState.toTarget:
            case CBState.toDrawpile:
            case CBState.to:
                float u = (Time.time - timeStart) / timeDuration;
                float uC = Easing.Ease (u, MOVE_EASING);
                if (u<0) {
                    transform.localPosition = bezierPts[0];
                    transform.rotation = bezierRots[0];
                    return;
                } else if (u>=1) {
                    uC = 1;
                    // Перевести из состояния to... в соответствующее следующее состояние
                    if (state == CBState.toHand) state = CBState.hand;
                    if (state == CBState.toTarget) state = CBState.target;
                    if (state == CBState.toDrawpile) state = CBState.drawpile;
                    if (state == CBState.to) state = CBState.idle;

                    // Переместить в конечное местоположение
                    transform.localPosition = bezierPts[bezierPts.Count - 1];
                    transform.rotation = bezierRots[bezierRots.Count - 1];

                    // Сбросить timeStart в 0, чтобы потом установить текущее время
                    timeStart = 0;

                    if (reportFinishTo != null) {
                        reportFinishTo.SendMessage("CBCallBack", this);
                        reportFinishTo = null;
                    } else if (callbackPlayer != null) {
                        callbackPlayer.CBCallBack(this);
                        callbackPlayer = null;
                    } else {}
                } else {
                    // Нормальный режим интерполяции (0 <= u < 1)
                    Vector3 pos = Utils.Bezier(uC, bezierPts);
                    transform.localPosition = pos;
                    Quaternion rotQ = Utils.Bezier(uC, bezierRots);
                    transform.rotation = rotQ;

                    if (u>0.5f) {
                        SpriteRenderer sRend = spriteRenderers[0];
                        if (sRend.sortingOrder != eventualSortOrder) {
                            // Установить конечный порядок сортировки
                            SetSortOrder(eventualSortOrder);
                        }
                        if (sRend.sortingLayerName != eventualSortLayer) {
                            // Установить конечный слой сортировки
                            SetSortingLayerName(eventualSortLayer);
                        }
                    }
                } 
                break;
        }   
    }

    // Этот метод определяет реакцию карты на щелчок мышью
    public override void OnMouseUpAsButton()
    {
        // Вызвать метод CardClicked объекта-одиночки Bartok
        Bartok.S.CardClicked(this);
        // Вызвать версию этого метода в базовом класса (Card.cs)
        base.OnMouseUpAsButton();
    }
}
