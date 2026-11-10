namespace Core.Domain;

// Додаткове завдання 3: явний стан партії.
//   Pending    – партію оформлено, але ще не прийнято на склад
//   Accepted   – прийнята, з неї можна видавати
//   Depleted   – видано все (залишок 0)
//   WrittenOff – списана (брак, прострочення); видавати не можна
public enum BatchStatus
{
    Pending,
    Accepted,
    Depleted,
    WrittenOff
}
