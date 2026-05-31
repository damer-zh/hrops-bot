using HROpsBot.Domain.Entities;

namespace HROpsBot.MockAPI;

/// <summary>Mock сервис оборудования (имитация ITSM/ServiceNow)</summary>
public class MockEquipmentService
{
    private static readonly List<EquipmentRequest> _requests = [];
    private static int _nextId = 1;

    public Task<EquipmentRequest> CreateRequestAsync(int employeeId, EquipmentType type)
    {
        var typeNames = new Dictionary<EquipmentType, (string Ru, string Kk)>
        {
            [EquipmentType.Laptop]   = ("Ноутбук", "Ноутбук"),
            [EquipmentType.Monitor]  = ("Монитор", "Монитор"),
            [EquipmentType.Keyboard] = ("Клавиатура", "Пернетақта"),
            [EquipmentType.Mouse]    = ("Мышь", "Тінтуір"),
            [EquipmentType.Headset]  = ("Гарнитура", "Гарнитура"),
            [EquipmentType.Phone]    = ("Телефон", "Телефон"),
            [EquipmentType.Chair]    = ("Кресло", "Орындық"),
            [EquipmentType.Desk]     = ("Стол", "Үстел"),
            [EquipmentType.Other]    = ("Другое оборудование", "Басқа жабдық")
        };

        var names = typeNames.GetValueOrDefault(type, ("Оборудование", "Жабдық"));
        var req = new EquipmentRequest
        {
            Id = _nextId++,
            EmployeeId = employeeId,
            Type = type,
            DescriptionRu = names.Item1,
            DescriptionKk = names.Item2,
            Status = RequestStatus.Pending,
            TicketNumber = $"IT-{Random.Shared.Next(10000, 99999)}",
            CreatedAt = DateTime.UtcNow
        };
        _requests.Add(req);
        return Task.FromResult(req);
    }

    public Task<EquipmentRequest?> GetRequestAsync(int id) =>
        Task.FromResult(_requests.FirstOrDefault(r => r.Id == id));

    public Task<List<EquipmentRequest>> GetEmployeeRequestsAsync(int employeeId) =>
        Task.FromResult(_requests.Where(r => r.EmployeeId == employeeId).ToList());

    public static (string Ru, string Kk) GetTypeName(EquipmentType type) =>
        type switch
        {
            EquipmentType.Laptop   => ("Ноутбук", "Ноутбук"),
            EquipmentType.Monitor  => ("Монитор", "Монитор"),
            EquipmentType.Keyboard => ("Клавиатура", "Пернетақта"),
            EquipmentType.Mouse    => ("Мышь", "Тінтуір"),
            EquipmentType.Headset  => ("Гарнитура", "Гарнитура"),
            EquipmentType.Phone    => ("Телефон", "Телефон"),
            EquipmentType.Chair    => ("Кресло", "Орындық"),
            EquipmentType.Desk     => ("Стол", "Үстел"),
            _                      => ("Оборудование", "Жабдық")
        };
}
