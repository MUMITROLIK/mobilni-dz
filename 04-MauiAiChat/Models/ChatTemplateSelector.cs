namespace AiChat.Models;

/// <summary>Вибирає шаблон повідомлення: свої повідомлення справа, повідомлення ШІ (і помилки) зліва.</summary>
public class ChatTemplateSelector : DataTemplateSelector
{
    public DataTemplate? AiTemplate { get; set; }
    public DataTemplate? UserTemplate { get; set; }

    protected override DataTemplate OnSelectTemplate(object item, BindableObject container) =>
        ((ChatLine)item).IsUser ? UserTemplate! : AiTemplate!;
}
