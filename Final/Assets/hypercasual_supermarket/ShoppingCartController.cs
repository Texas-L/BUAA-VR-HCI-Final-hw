using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

[RequireComponent(typeof(RectTransform))]
public class ShoppingCartController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject cartItemPrefab;
    public Transform[] cartPages;
    [Space(10)]
    public Button clearAllButton;
    public Button buyAllButton;
    public Button prevPageButton;
    public Button nextPageButton;

    [Header("Settings")]
    [SerializeField, Range(1, 10)]
    private int itemsPerPage = 5;

    private List<CartItem> cartItems = new List<CartItem>();
    private int currentPage = 0;

    void Start()
    {
        ValidateReferences();
        InitializeButtons();
        UpdatePageVisibility();
        AddTestItems();
    }

    void AddTestItems()
    {
        AddItem("苹果", 5.99f, 2);
        AddItem("香蕉", 3.49f, 1);
        AddItem("橙子", 4.99f, 3);
    }

    void ValidateReferences()
    {
        if (cartPages == null || cartPages.Length == 0)
        {
            var pages = new List<Transform>();
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Page"))
                {
                    pages.Add(child);
                }
            }

            if (pages.Count > 0)
            {
                cartPages = pages.ToArray();
                UnityEngine.Debug.LogWarning($"自动分配了 {pages.Count} 个分页");
            }
            else
            {
                UnityEngine.Debug.LogError("未设置分页！请创建分页对象并拖入数组");
                enabled = false;
            }
        }
    }

    void InitializeButtons()
    {
        clearAllButton.onClick.AddListener(ClearAllItems);
        buyAllButton.onClick.AddListener(BuyAllItems);
        prevPageButton.onClick.AddListener(ShowPrevPage);
        nextPageButton.onClick.AddListener(ShowNextPage);
    }

    public void AddItem(string itemName, float price, int quantity = 1)
    {
        CartItem existingItem = cartItems.Find(item => item.Name == itemName);

        if (existingItem != null)
        {
            existingItem.Quantity += quantity;
            UpdateItemUI(existingItem);
        }
        else
        {
            int targetPage = Mathf.Min(cartItems.Count / itemsPerPage, cartPages.Length - 1);
            CartItem newItem = new CartItem(itemName, price, quantity, targetPage);
            cartItems.Add(newItem);
            CreateItemUI(newItem);
        }

        UpdatePageVisibility();
    }

    void CreateItemUI(CartItem item)
    {
        if (!IsValidPageIndex(item.PageIndex)) return;

        GameObject newItem = Instantiate(cartItemPrefab, cartPages[item.PageIndex]);
        item.UIObject = newItem;

        if (TryGetTextComponent(newItem, "ItemInfo", out UnityEngine.UI.Text itemText))
        {
            itemText.text = $"{item.Name} ×{item.Quantity} ¥{item.Price * item.Quantity:F2}";
        }

        if (TryGetButton(newItem, "DeleteButton", out Button deleteButton))
        {
            deleteButton.onClick.AddListener(() => RemoveItem(item));
        }
    }

    void UpdateItemUI(CartItem item)
    {
        if (item.UIObject != null &&
            TryGetTextComponent(item.UIObject, "ItemInfo", out UnityEngine.UI.Text itemText))
        {
            itemText.text = $"{item.Name} ×{item.Quantity} ¥{item.Price * item.Quantity:F2}";
        }
    }

    void RemoveItem(CartItem item)
    {
        if (cartItems.Remove(item) && item.UIObject != null)
        {
            Destroy(item.UIObject);
            ReorganizeItems();
        }
    }

    void ClearAllItems()
    {
        foreach (var item in cartItems)
        {
            if (item.UIObject != null) Destroy(item.UIObject);
        }
        cartItems.Clear();
        currentPage = 0;
        UpdatePageVisibility();
    }

    void BuyAllItems()
    {
        UnityEngine.Debug.Log($"购买 {cartItems.Count} 件商品，总价: {CalculateTotalPrice():F2} 元");
        ClearAllItems();
    }

    float CalculateTotalPrice()
    {
        float total = 0;
        foreach (var item in cartItems) total += item.Price * item.Quantity;
        return total;
    }

    void ReorganizeItems()
    {
        foreach (Transform page in cartPages)
        {
            foreach (Transform child in page)
            {
                if (child.name.Contains("(Clone)")) Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < cartItems.Count; i++)
        {
            cartItems[i].PageIndex = Mathf.Min(i / itemsPerPage, cartPages.Length - 1);
            CreateItemUI(cartItems[i]);
        }

        currentPage = Mathf.Clamp(currentPage, 0, cartPages.Length - 1);
        UpdatePageVisibility();
    }

    void ShowPrevPage()
    {
        if (currentPage > 0) UpdatePageVisibility(--currentPage);
    }

    void ShowNextPage()
    {
        if (currentPage < cartPages.Length - 1) UpdatePageVisibility(++currentPage);
    }

    void UpdatePageVisibility(int? specificPage = null)
    {
        currentPage = specificPage ?? currentPage;
        for (int i = 0; i < cartPages.Length; i++)
        {
            cartPages[i].gameObject.SetActive(i == currentPage);
        }
        prevPageButton.interactable = currentPage > 0;
        nextPageButton.interactable = currentPage < cartPages.Length - 1;
    }

    bool TryGetTextComponent(GameObject obj, string childName, out UnityEngine.UI.Text text)
    {
        Transform child = obj.transform.Find(childName);
        if (child != null && (text = child.GetComponent<UnityEngine.UI.Text>()) != null)
            return true;

        UnityEngine.Debug.LogError($"找不到文本组件: {childName}");
        text = null;
        return false;
    }

    bool TryGetButton(GameObject obj, string childName, out Button button)
    {
        Transform child = obj.transform.Find(childName);
        if (child != null && (button = child.GetComponent<Button>()) != null)
            return true;

        UnityEngine.Debug.LogError($"找不到按钮: {childName}");
        button = null;
        return false;
    }

    bool IsValidPageIndex(int index)
    {
        if (index >= 0 && index < cartPages.Length) return true;
        UnityEngine.Debug.LogError($"无效页面索引: {index}");
        return false;
    }
}

[System.Serializable]
public class CartItem
{
    public string Name;
    public float Price;
    public int Quantity;
    [System.NonSerialized] public GameObject UIObject;
    public int PageIndex;

    public CartItem(string name, float price, int quantity, int pageIndex)
    {
        Name = name;
        Price = price;
        Quantity = quantity;
        PageIndex = pageIndex;
    }
}