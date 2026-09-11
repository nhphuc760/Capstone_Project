using System;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerUI
{
    void Open();
    void Close();
}


public class PlayerUIComponent : MonoBehaviour
{

    public enum OpenUI 
    {
        None,
        RadialMenu,
        Inventory,
        BuildMenu
    }


    Dictionary<OpenUI, IPlayerUI> _uis = new();
    IPlayerUI _currentUI;
    

    public OpenUI ActiveUI { get; private set; } = OpenUI.None;

        


    private void Update()
    {
        HandleInput();
    }



    void HandleInput()
    {
        if ((Input.GetKeyDown(KeyCode.BackQuote) || Input.GetKeyUp(KeyCode.BackQuote)))
        {
            Toggle(OpenUI.RadialMenu);
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            Toggle(OpenUI.Inventory);
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            Toggle(OpenUI.BuildMenu);
        }

    }


    public void Toggle(OpenUI ui)
    {
        if (IsActive(ui))
        {
            CloseCurrent();
        }
        else
        {
            Open(ui);
        }
    }

    public void CloseCurrent()
    {
        if (_currentUI == null)
            return;

        _currentUI.Close();

        _currentUI = null;
        ActiveUI = OpenUI.None;
    }

    public void Open(OpenUI ui)
    {
        if (ui == OpenUI.None)
            return;

        // Đang có UI khác mở → không cho mở UI mới
        if (ActiveUI != OpenUI.None)
            return;

        if (!_uis.TryGetValue(ui, out var target))
        {
            Debug.LogWarning($"UI {ui} has not been registered.");
            return;
        }

        _currentUI = target;
        ActiveUI = ui;

        _currentUI.Open();
    }

    public bool IsActive(OpenUI ui) => ActiveUI == ui;

    public void RegisterPlayerUI(OpenUI ui, IPlayerUI entity)
    {
        _uis.Add(ui, entity);
    }



}
