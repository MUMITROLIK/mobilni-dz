namespace AiChat.Models;

/// <summary>Одне повідомлення в розмові: від користувача, від ШІ або повідомлення про помилку.</summary>
public class ChatLine
{
    public bool IsUser { get; init; }
    public bool IsError { get; init; }

    /// <summary>Службове повідомлення (вітання): показуємо, але не надсилаємо моделі.</summary>
    public bool IsLocal { get; init; }
    public string Text { get; init; } = "";
    public string Time { get; init; } = DateTime.Now.ToString("HH:mm");

    /// <summary>Колір «бульбашки»: користувач - бірюзовий, ШІ - сірувато-синій, помилка - червоний.</summary>
    public Color Bubble => IsError ? Color.FromArgb("#B13E53") : IsUser ? Color.FromArgb("#257179") : Color.FromArgb("#333C57");
}
