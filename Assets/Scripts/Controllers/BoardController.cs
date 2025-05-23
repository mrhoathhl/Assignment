using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };
    public bool IsBusy { get; private set; }

    private Board m_board;
    private GameManager m_gameManager;
    private GameSettings m_gameSettings;
    private Camera m_cam;

    private bool m_gameOver;
    private bool m_isDragging;
    private bool m_hintIsShown;
    private float m_timeAfterFill;

    private Collider2D m_hitCollider;
    private List<Cell> m_potentialMatch = new  List<Cell>();

    public void StartGame(GameManager gameManager, GameSettings gameSettings, ItemManagerSO itemManager)
    {
        m_gameManager = gameManager;
        m_gameSettings = gameSettings;

        m_cam = Camera.main;
        m_gameManager.StateChangedAction += OnGameStateChange;

        m_board = new Board(transform, gameSettings, itemManager);
        FillBoard();
    }

    public void Update()
    {
        if (m_gameOver || IsBusy) return;

        HandleHintTimer();
        HandleInput();
    }

    #region Game State

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                m_gameOver = true;
                StopHints();
                break;
        }
    }

    #endregion

    #region Input Handling

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var hit = RaycastMouse();
            if (hit) { m_isDragging = true; m_hitCollider = hit.collider; }
        }

        if (Input.GetMouseButtonUp(0)) ResetRaycast();

        if (Input.GetMouseButton(0) && m_isDragging)
        {
            var hit = RaycastMouse();
            if (!hit) { ResetRaycast(); return; }

            if (m_hitCollider && m_hitCollider != hit.collider)
            {
                TrySwap(m_hitCollider.GetComponent<Cell>(), hit.collider.GetComponent<Cell>());
                ResetRaycast();
            }
        }
    }

    private RaycastHit2D RaycastMouse()
    {
        return Physics2D.Raycast(m_cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
    }

    private void ResetRaycast()
    {
        m_isDragging = false;
        m_hitCollider = null;
    }

    #endregion

    #region Fill, Match & Collapse

    private void FillBoard()
    {
        m_board.Fill();
        TryCollapseInitialMatch();
    }

    private void TryCollapseInitialMatch()
    {
        List<Cell> matches = m_board.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            m_potentialMatch = m_board.GetPotentialMatches();
            if (m_potentialMatch.Count == 0)
                StartCoroutine(ShuffleBoardCoroutine());
            else
                IsBusy = false;
        }

        m_timeAfterFill = 0f;
    }

    private void TrySwap(Cell c1, Cell c2)
    {
        if (!c1.IsNeighbour(c2)) return;

        StopHints();
        IsBusy = true;
        SetSortingLayer(c1, c2);

        m_board.Swap(c1, c2, () => FindMatchesAfterSwap(c1, c2));
    }

    private void FindMatchesAfterSwap(Cell c1, Cell c2)
    {
        if (TryHandleBonus(c1) || TryHandleBonus(c2)) return;

        var matches = GetMatches(c1).Union(GetMatches(c2)).Distinct().ToList();

        if (matches.Count < m_gameSettings.MatchesMin)
        {
            m_board.Swap(c1, c2, () => IsBusy = false);
        }
        else
        {
            OnMoveEvent();
            CollapseMatches(matches, c2);
        }
    }

    private bool TryHandleBonus(Cell cell)
    {
        if (!(cell.Item is BonusItem)) return false;

        cell.ExplodeItem();
        StartCoroutine(ShiftDownItemsCoroutine());
        return true;
    }

    private List<Cell> GetMatches(Cell cell)
    {
        var hor = m_board.GetHorizontalMatches(cell);
        var ver = m_board.GetVerticalMatches(cell);

        return hor.Count >= m_gameSettings.MatchesMin || ver.Count >= m_gameSettings.MatchesMin
            ? hor.Concat(ver).Distinct().ToList()
            : new List<Cell>();
    }

    private void CollapseMatches(List<Cell> matches, Cell refCell)
    {
        foreach (var cell in matches)
            cell.ExplodeItem();

        if (matches.Count > m_gameSettings.MatchesMin)
            m_board.ConvertNormalToBonus(matches, refCell);

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        m_board.ShiftDownItems();
        yield return new WaitForSeconds(0.2f);

        m_board.FillGapsWithNewItems();
        yield return new WaitForSeconds(0.2f);

        TryCollapseInitialMatch();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        m_board.Shuffle();
        yield return new WaitForSeconds(0.3f);
        TryCollapseInitialMatch();
    }

    #endregion

    #region Hint

    private void HandleHintTimer()
    {
        if (m_hintIsShown || m_potentialMatch.Count == 0) return;

        m_timeAfterFill += Time.deltaTime;
        if (m_timeAfterFill > m_gameSettings.TimeForHint)
        {
            m_timeAfterFill = 0f;
            ShowHint();
        }
    }

    private void ShowHint()
    {
        m_hintIsShown = true;
        foreach (var cell in m_potentialMatch)
            cell.AnimateItemForHint();
    }

    private void StopHints()
    {
        m_hintIsShown = false;
        foreach (var cell in m_potentialMatch)
            cell.StopHintAnimation();

        m_potentialMatch.Clear();
    }

    #endregion

    #region Utility

    private void SetSortingLayer(Cell c1, Cell c2)
    {
        c1.Item?.SetSortingLayerHigher();
        c2.Item?.SetSortingLayerLower();
    }

    internal void Clear()
    {
        m_board.Clear();
    }

    #endregion
}
