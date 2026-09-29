using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using Binding = System.Windows.Data.Binding;
using BindingMode = System.Windows.Data.BindingMode;

namespace DailyLogAssistant.Localization;

public sealed class LocalizationService : INotifyPropertyChanged
{
    private static readonly IReadOnlyDictionary<string, (string Vietnamese, string Russian)> Texts =
        new Dictionary<string, (string, string)>(StringComparer.Ordinal)
        {
            ["Dashboard"] = ("Tổng quan", "Главная"),
            ["Work  +"] = ("Công việc  +", "Работа  +"),
            ["Personal  +"] = ("Cá nhân  +", "Личное  +"),
            ["Letters  +"] = ("Thư  +", "Письма  +"),
            ["Notes  +"] = ("Ghi chú  +", "Заметки  +"),
            ["Tags"] = ("Thẻ", "Теги"),
            ["Calendar"] = ("Lịch", "Календарь"),
            ["History"] = ("Lịch sử", "История"),
            ["Export"] = ("Xuất dữ liệu", "Экспорт"),
            ["Statistics"] = ("Thống kê", "Статистика"),
            ["Settings"] = ("Cài đặt", "Настройки"),
            ["CUSTOM CATEGORIES"] = ("DANH MỤC TÙY CHỈNH", "ПОЛЬЗОВАТЕЛЬСКИЕ КАТЕГОРИИ"),
            ["PERSONAL LOGS"] = ("NHẬT KÝ CÁ NHÂN", "ЛИЧНЫЕ ЗАПИСИ"),
            ["WORK"] = ("CÔNG VIỆC", "РАБОТА"),
            ["PERSONAL"] = ("CÁ NHÂN", "ЛИЧНОЕ"),
            ["LETTER"] = ("THƯ", "ПИСЬМО"),
            ["NOTE"] = ("GHI CHÚ", "ЗАМЕТКА"),
            ["+ Add custom category"] = ("+ Thêm danh mục", "+ Добавить категорию"),
            ["Quick add"] = ("Ghi nhanh", "Быстрая запись"),
            ["＋ Quick add"] = ("＋ Ghi nhanh", "＋ Быстрая запись"),
            ["New work log"] = ("Nhật ký công việc mới", "Новая рабочая запись"),
            ["Your day in review"] = ("Tổng kết ngày của bạn", "Итоги вашего дня"),
            ["LOGS TODAY"] = ("NHẬT KÝ HÔM NAY", "ЗАПИСИ ЗА СЕГОДНЯ"),
            ["WORK THIS WEEK"] = ("CÔNG VIỆC TUẦN NÀY", "РАБОТА ЗА НЕДЕЛЮ"),
            ["WORK STREAK"] = ("CHUỖI NGÀY LÀM VIỆC", "СЕРИЯ РАБОЧИХ ДНЕЙ"),
            ["days"] = ("ngày", "дн."),
            ["Work today"] = ("Công việc hôm nay", "Работа сегодня"),
            ["Recent logs"] = ("Nhật ký gần đây", "Недавние записи"),
            ["No logs yet. Use Quick add to capture your first entry."] =
                ("Chưa có nhật ký. Hãy dùng Ghi nhanh để tạo mục đầu tiên.", "Записей пока нет. Создайте первую с помощью быстрой записи."),
            ["Open selected log"] = ("Mở nhật ký đã chọn", "Открыть выбранную запись"),
            ["Export weekly report"] = ("Xuất báo cáo tuần", "Экспорт недельного отчёта"),
            ["Today"] = ("Hôm nay", "Сегодня"),
            ["‹"] = ("‹", "‹"),
            ["›"] = ("›", "›"),
            ["Previous month"] = ("Tháng trước", "Предыдущий месяц"),
            ["Next month"] = ("Tháng sau", "Следующий месяц"),
            ["Mon"] = ("T2", "Пн"),
            ["Tue"] = ("T3", "Вт"),
            ["Wed"] = ("T4", "Ср"),
            ["Thu"] = ("T5", "Чт"),
            ["Fri"] = ("T6", "Пт"),
            ["Sat"] = ("T7", "Сб"),
            ["Sun"] = ("CN", "Вс"),
            ["Edit selected"] = ("Sửa mục đã chọn", "Изменить выбранное"),
            ["No logs for this day."] = ("Ngày này chưa có nhật ký.", "На этот день записей нет."),
            ["Log entry"] = ("Mục nhật ký", "Запись"),
            ["Preview letter"] = ("Xem trước thư", "Предпросмотр письма"),
            ["Export letter TXT"] = ("Xuất thư TXT", "Экспорт письма TXT"),
            ["Export letter Markdown"] = ("Xuất thư Markdown", "Экспорт письма Markdown"),
            ["Category"] = ("Danh mục", "Категория"),
            ["Type"] = ("Loại", "Тип"),
            ["Date"] = ("Ngày", "Дата"),
            ["Title / Task"] = ("Tiêu đề / Công việc", "Заголовок / Задача"),
            ["Project"] = ("Dự án", "Проект"),
            ["Status"] = ("Trạng thái", "Статус"),
            ["Person / recipient"] = ("Người nhận", "Получатель"),
            ["Mood"] = ("Tâm trạng", "Настроение"),
            ["Opening"] = ("Lời mở đầu", "Обращение"),
            ["Body / content"] = ("Nội dung", "Содержание"),
            ["Closing"] = ("Lời kết", "Заключение"),
            ["Signature"] = ("Chữ ký", "Подпись"),
            ["Things to remember"] = ("Điều cần nhớ", "Что нужно помнить"),
            ["Result"] = ("Kết quả", "Результат"),
            ["Problems / blockers"] = ("Vấn đề / Trở ngại", "Проблемы / Препятствия"),
            ["Notes"] = ("Ghi chú", "Заметки"),
            ["Tags separated by commas"] = ("Thẻ phân cách bằng dấu phẩy", "Теги через запятую"),
            ["Snooze 15 min"] = ("Hoãn 15 phút", "Отложить на 15 мин"),
            ["Snooze 30 min"] = ("Hoãn 30 phút", "Отложить на 30 мин"),
            ["Snooze 1 hour"] = ("Hoãn 1 giờ", "Отложить на 1 час"),
            ["Dismiss today"] = ("Bỏ qua hôm nay", "Не напоминать сегодня"),
            ["Save log"] = ("Lưu nhật ký", "Сохранить запись"),
            ["Create or rename tag"] = ("Tạo hoặc đổi tên thẻ", "Создать тег или переименовать"),
            ["Name"] = ("Tên", "Название"),
            ["Color (hex)"] = ("Màu (mã hex)", "Цвет (hex-код)"),
            ["Save tag"] = ("Lưu thẻ", "Сохранить тег"),
            ["Delete selected"] = ("Xóa mục đã chọn", "Удалить выбранное"),
            ["Search logs"] = ("Tìm nhật ký", "Поиск по записям"),
            ["All time"] = ("Toàn bộ thời gian", "За всё время"),
            ["This week"] = ("Tuần này", "Эта неделя"),
            ["This month"] = ("Tháng này", "Этот месяц"),
            ["Custom range"] = ("Khoảng tùy chỉnh", "Выбрать период"),
            ["All categories"] = ("Tất cả danh mục", "Все категории"),
            ["All tags"] = ("Tất cả thẻ", "Все теги"),
            ["All statuses"] = ("Tất cả trạng thái", "Все статусы"),
            ["Planned"] = ("Đã lên kế hoạch", "Запланировано"),
            ["In Progress"] = ("Đang thực hiện", "В процессе"),
            ["Completed"] = ("Hoàn thành", "Выполнено"),
            ["Blocked"] = ("Bị chặn", "Заблокировано"),
            ["Cancelled"] = ("Đã hủy", "Отменено"),
            ["From"] = ("Từ ngày", "С"),
            ["To"] = ("Đến ngày", "По"),
            ["Edit"] = ("Sửa", "Изменить"),
            ["Duplicate"] = ("Nhân bản", "Дублировать"),
            ["Delete"] = ("Xóa", "Удалить"),
            ["Updated"] = ("Cập nhật", "Обновлено"),
            ["Export center"] = ("Trung tâm xuất dữ liệu", "Центр экспорта"),
            ["Format"] = ("Định dạng", "Формат"),
            ["Tag filter"] = ("Lọc theo thẻ", "Фильтр по тегу"),
            ["Date from"] = ("Từ ngày", "Дата с"),
            ["Date to"] = ("Đến ngày", "Дата по"),
            ["Excel exports a formatted weekly Work report; CSV and Markdown export filtered logs. Letter TXT export is available in the letter editor."] =
                ("Excel xuất báo cáo công việc tuần; CSV và Markdown xuất nhật ký đã lọc. Có thể xuất thư TXT trong trình soạn thảo thư.", "Excel экспортирует недельный отчёт о работе; CSV и Markdown — отфильтрованные записи. Экспорт письма в TXT доступен в редакторе письма."),
            ["Weekly Work Report"] = ("Báo cáo công việc tuần", "Недельный отчёт о работе"),
            ["Total logs: {0}"] = ("Tổng số nhật ký: {0}", "Всего записей: {0}"),
            ["Total logs"] = ("Tổng số nhật ký", "Всего записей"),
            ["Current work streak: {0} days"] = ("Chuỗi ngày làm việc hiện tại: {0} ngày", "Текущая серия рабочих дней: {0} дн."),
            ["Current work streak"] = ("Chuỗi ngày làm việc hiện tại", "Текущая серия рабочих дней"),
            ["Longest streak: {0} days"] = ("Chuỗi dài nhất: {0} ngày", "Самая длинная серия: {0} дн."),
            ["Longest streak"] = ("Chuỗi dài nhất", "Самая длинная серия"),
            ["Logs this month: {0}"] = ("Nhật ký tháng này: {0}", "Записей за месяц: {0}"),
            ["Logs this month"] = ("Nhật ký tháng này", "Записей за месяц"),
            ["Work completion rate: {0}%"] = ("Tỷ lệ hoàn thành công việc: {0}%", "Выполнение рабочих задач: {0}%"),
            ["Work completion rate"] = ("Tỷ lệ hoàn thành công việc", "Выполнение рабочих задач"),
            ["Logs this year: {0}"] = ("Nhật ký năm nay: {0}", "Записей за год: {0}"),
            ["Logs this year"] = ("Nhật ký năm nay", "Записей за год"),
            ["Work reminder time in local 24-hour format"] = ("Giờ nhắc công việc (giờ địa phương, định dạng 24 giờ)", "Время напоминания о работе (местное, 24-часовой формат)"),
            ["Enable daily work reminder"] = ("Bật nhắc công việc hằng ngày", "Ежедневное напоминание о работе"),
            ["Start application with Windows"] = ("Khởi động ứng dụng cùng Windows", "Запускать приложение вместе с Windows"),
            ["Minimize to tray"] = ("Thu nhỏ xuống khay hệ thống", "Сворачивать в область уведомлений"),
            ["Launch log editor automatically"] = ("Tự động mở trình soạn nhật ký", "Автоматически открывать редактор записей"),
            ["Show Windows notification"] = ("Hiển thị thông báo Windows", "Показывать уведомления Windows"),
            ["Theme"] = ("Giao diện", "Тема"),
            ["System"] = ("Theo hệ thống", "Системная"),
            ["Light"] = ("Sáng", "Светлая"),
            ["Dark"] = ("Tối", "Тёмная"),
            ["Accent color"] = ("Màu nhấn", "Цвет акцента"),
            ["Work report template (optional)"] = ("Mẫu báo cáo công việc (không bắt buộc)", "Шаблон отчёта о работе (необязательно)"),
            ["Browse..."] = ("Duyệt...", "Обзор..."),
            ["Save settings"] = ("Lưu cài đặt", "Сохранить настройки"),
            ["Test reminder now"] = ("Thử nhắc ngay", "Проверить напоминание"),
            ["Backup database"] = ("Sao lưu cơ sở dữ liệu", "Резервная копия базы данных"),
            ["Create custom category"] = ("Tạo danh mục tùy chỉnh", "Создать категорию"),
            ["Work Log Reminder"] = ("Nhắc ghi nhật ký công việc", "Напоминание о рабочей записи"),
            ["Time to record your work today."] = ("Đã đến lúc ghi lại công việc hôm nay.", "Пора записать сегодняшние рабочие задачи."),
            ["Language"] = ("Ngôn ngữ", "Язык"),
            ["English"] = ("English", "Английский"),
            ["Tiếng Việt"] = ("Tiếng Việt", "Вьетнамский"),
            ["Русский"] = ("Русский", "Русский"),
            ["Log type"] = ("Loại nhật ký", "Тип записи"),
            ["Title"] = ("Tiêu đề", "Заголовок"),
            ["Main content"] = ("Nội dung chính", "Основное содержание"),
            ["Work status (used for Work logs)"] = ("Trạng thái (áp dụng cho nhật ký công việc)", "Статус (для рабочих записей)"),
            ["Cancel"] = ("Hủy", "Отмена"),
            ["Work log pending"] = ("Chưa ghi nhật ký công việc", "Рабочая запись не создана"),
            ["Work log completed"] = ("Đã ghi nhật ký công việc", "Рабочая запись создана"),
            ["completed"] = ("đã hoàn thành", "выполнено"),
            ["No work logs this week"] = ("Tuần này chưa có nhật ký công việc", "На этой неделе рабочих записей нет"),
            ["No work entry yet. Record today's progress."] = ("Chưa có mục công việc hôm nay. Hãy ghi lại tiến độ.", "Рабочей записи за сегодня пока нет. Запишите результаты."),
            ["No work logs yet"] = ("Chưa có nhật ký công việc", "Рабочих записей пока нет"),
            ["{0} of {1} completed"] = ("Đã hoàn thành {0}/{1}", "Выполнено: {0} из {1}"),
            ["Work reminder at {0}"] = ("Nhắc công việc lúc {0}", "Напоминание о работе в {0}"),
            ["Ready"] = ("Sẵn sàng", "Готово"),
            ["Log saved successfully."] = ("Đã lưu nhật ký.", "Запись сохранена."),
            ["Could not save this log."] = ("Không thể lưu nhật ký.", "Не удалось сохранить запись."),
            ["Log saved, but the views could not be refreshed."] = ("Đã lưu nhật ký nhưng không thể làm mới giao diện.", "Запись сохранена, но не удалось обновить представления."),
            ["Log deleted."] = ("Đã xóa nhật ký.", "Запись удалена."),
            ["Settings saved."] = ("Đã lưu cài đặt.", "Настройки сохранены."),
            ["Tag saved."] = ("Đã lưu thẻ.", "Тег сохранён."),
            ["Tag deleted."] = ("Đã xóa thẻ.", "Тег удалён."),
            ["Custom category created."] = ("Đã tạo danh mục tùy chỉnh.", "Категория создана."),
            ["Could not refresh history."] = ("Không thể làm mới lịch sử.", "Не удалось обновить историю."),
            ["Reminder dismissed for today."] = ("Đã bỏ nhắc nhở trong hôm nay.", "Напоминание отключено на сегодня."),
            ["Work reminder snoozed for {0} minutes."] = ("Đã hoãn nhắc nhở công việc {0} phút.", "Напоминание о работе отложено на {0} мин."),
            ["Select a log type."] = ("Hãy chọn loại nhật ký.", "Выберите тип записи."),
            ["Enter a valid time such as 17:00."] = ("Hãy nhập giờ hợp lệ, ví dụ 17:00.", "Введите корректное время, например 17:00."),
            ["Personal Log Manager"] = ("Personal Log Manager", "Personal Log Manager"),
            ["Personal Log Manager could not start."] = ("Không thể khởi động Personal Log Manager.", "Не удалось запустить Personal Log Manager."),
            ["An unexpected error occurred. Your saved logs are safe."] = ("Đã xảy ra lỗi ngoài dự kiến. Nhật ký đã lưu vẫn an toàn.", "Произошла непредвиденная ошибка. Сохранённые записи в безопасности."),
            ["An unexpected error occurred. Your saved logs are safe.\n\n{0}"] = ("Đã xảy ra lỗi ngoài dự kiến. Nhật ký đã lưu vẫn an toàn.\n\n{0}", "Произошла непредвиденная ошибка. Сохранённые записи в безопасности.\n\n{0}"),
            ["Personal Log Manager could not start.\n\n{0}"] = ("Không thể khởi động Personal Log Manager.\n\n{0}", "Не удалось запустить Personal Log Manager.\n\n{0}"),
            ["Please add a title or some content before saving."] = ("Hãy nhập tiêu đề hoặc nội dung trước khi lưu.", "Перед сохранением добавьте заголовок или содержание."),
            ["Select a valid category."] = ("Hãy chọn danh mục hợp lệ.", "Выберите корректную категорию."),
            ["The selected log no longer exists."] = ("Nhật ký đã chọn không còn tồn tại.", "Выбранная запись больше не существует."),
            ["Please write at least something about your day."] = ("Hãy ghi lại ít nhất một nội dung trong ngày.", "Добавьте хотя бы что-нибудь о своём дне."),
            ["Tag name is required."] = ("Cần nhập tên thẻ.", "Укажите название тега."),
            ["Tag names must be 80 characters or fewer."] = ("Tên thẻ không được dài quá 80 ký tự.", "Название тега не должно превышать 80 символов."),
            ["Use a six-digit hex color such as #315C4C."] = ("Hãy dùng mã màu hex gồm sáu chữ số, ví dụ #315C4C.", "Укажите шестизначный hex-код цвета, например #315C4C."),
            ["Category name is required."] = ("Cần nhập tên danh mục.", "Укажите название категории."),
            ["Category names must be 80 characters or fewer."] = ("Tên danh mục không được dài quá 80 ký tự.", "Название категории не должно превышать 80 символов."),
            ["Provide a short category icon."] = ("Hãy nhập biểu tượng danh mục ngắn.", "Укажите короткий значок категории."),
            ["A category with that name already exists."] = ("Danh mục này đã tồn tại.", "Категория с таким названием уже существует."),
            ["The selected category no longer exists."] = ("Danh mục đã chọn không còn tồn tại.", "Выбранная категория больше не существует."),
            ["Built-in categories cannot be deleted."] = ("Không thể xóa danh mục có sẵn.", "Встроенные категории нельзя удалить."),
            ["Move or delete this category's logs before deleting it."] = ("Hãy chuyển hoặc xóa nhật ký thuộc danh mục này trước.", "Сначала переместите или удалите записи этой категории."),
            ["The report end date must not be before the start date."] = ("Ngày kết thúc báo cáo không được trước ngày bắt đầu.", "Дата окончания отчёта не может быть раньше даты начала."),
            ["Windows could not locate the application executable."] = ("Windows không tìm thấy tệp thực thi của ứng dụng.", "Windows не удалось найти исполняемый файл приложения."),
            ["Delete '{0}'?"] = ("Xóa '{0}'?", "Удалить «{0}»?"),
            ["Delete log"] = ("Xóa nhật ký", "Удаление записи"),
            ["Delete tag"] = ("Xóa thẻ", "Удаление тега"),
            ["Delete tag '{0}'?"] = ("Xóa thẻ '{0}'?", "Удалить тег «{0}»?"),
            ["Weekly Excel reports are available for the WORK category."] = ("Báo cáo Excel tuần chỉ khả dụng cho danh mục WORK.", "Недельные отчёты Excel доступны только для категории WORK."),
            ["Choose the LETTER category to export a letter."] = ("Hãy chọn danh mục LETTER để xuất thư.", "Для экспорта письма выберите категорию LETTER."),
            ["Choose the LETTER category to preview a letter."] = ("Hãy chọn danh mục LETTER để xem trước thư.", "Для предпросмотра письма выберите категорию LETTER."),
            ["Export completed successfully."] = ("Xuất dữ liệu thành công.", "Экспорт завершён."),
            ["Export failed."] = ("Xuất dữ liệu thất bại.", "Не удалось выполнить экспорт."),
            ["Open"] = ("Mở", "Открыть"),
            ["New category"] = ("Danh mục mới", "Новая категория"),
            ["The app is still running in the notification area."] =
                ("Ứng dụng vẫn đang chạy trong khay hệ thống.", "Приложение продолжает работать в области уведомлений."),
            ["Today's Work Log"] = ("Nhật ký công việc hôm nay", "Рабочая запись за сегодня"),
            ["New Log"] = ("Nhật ký mới", "Новая запись"),
            ["Exit"] = ("Thoát", "Выход"),
            ["Letter preview"] = ("Xem trước thư", "Предпросмотр письма"),
            ["Preview"] = ("Xem trước", "Предпросмотр"),
            ["Icon"] = ("Biểu tượng", "Значок"),
            ["Create"] = ("Tạo", "Создать")
        };

    public static LocalizationService Instance { get; } = new();

    private string _language = "English";

    public string Language
    {
        get => _language;
        private set
        {
            if (_language == value) return;
            _language = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CurrentCulture));
            OnPropertyChanged(nameof(CurrentXmlLanguage));
        }
    }

    public CultureInfo CurrentCulture => Language switch
    {
        "Tiếng Việt" => CultureInfo.GetCultureInfo("vi-VN"),
        "Русский" => CultureInfo.GetCultureInfo("ru-RU"),
        _ => CultureInfo.GetCultureInfo("en-US")
    };

    public XmlLanguage CurrentXmlLanguage => XmlLanguage.GetLanguage(CurrentCulture.IetfLanguageTag);

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler? LanguageChanged;

    public static string Translate(string? text)
    {
        if (string.IsNullOrEmpty(text) || !Texts.TryGetValue(text, out var translation)) return text ?? "";
        return Instance.Language switch
        {
            "Tiếng Việt" => translation.Vietnamese,
            "Русский" => translation.Russian,
            _ => text
        };
    }

    public static string TranslateException(Exception exception)
    {
        var message = exception.Message;
        if (exception is ArgumentException)
        {
            var parameterSuffix = message.LastIndexOf(" (Parameter ", StringComparison.Ordinal);
            if (parameterSuffix >= 0) message = message[..parameterSuffix];
        }
        return Translate(message);
    }

    public static void SetLanguage(string? language)
    {
        var normalized = language is "Tiếng Việt" or "Русский" ? language : "English";
        var instance = Instance;
        if (instance.Language == normalized) return;
        instance.Language = normalized;
        CultureInfo.CurrentCulture = instance.CurrentCulture;
        CultureInfo.CurrentUICulture = instance.CurrentCulture;
        instance.LanguageChanged?.Invoke(instance, EventArgs.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

[ValueConversion(typeof(string), typeof(string))]
public sealed class LocalizedTextConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length > 0 && values[0] is string text ? LocalizationService.Translate(text) : DependencyProperty.UnsetValue;

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

[MarkupExtensionReturnType(typeof(object))]
public sealed class TranslateExtension(string text) : MarkupExtension
{
    [ConstructorArgument("text")]
    public string Text { get; set; } = text;

    public override object ProvideValue(IServiceProvider serviceProvider) =>
        new Binding(nameof(LocalizationService.Language))
        {
            Source = LocalizationService.Instance,
            Mode = BindingMode.OneWay,
            Converter = new TextConverter(),
            ConverterParameter = Text
        }.ProvideValue(serviceProvider);

    private sealed class TextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            LocalizationService.Translate(parameter as string);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            Binding.DoNothing;
    }
}
