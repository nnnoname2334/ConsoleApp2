using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TodoClient
{
    public class Todo
    {
        [JsonPropertyName("userId")]
        public int UserId { get; set; }

        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("completed")
        public bool Completed { get; set; }

        public override string ToString()
        {
            return $"ID: {Id} | Пользователь: {UserId} | {Title} | Статус: {(Completed ? "выполнена" : "не выполнена")}";
        }
    }

    public class TodoService
    {
        private readonly HttpClient _client;

        public TodoService(HttpClient client)
        {
            _client = client;
        }

        // GET /todos?userId={id}
        public async Task<List<Todo>> GetByUserAsync(int userId)
        {
            HttpResponseMessage response = await _client.GetAsync($"/todos?userId={userId}");
            response.EnsureSuccessStatusCode();
            List<Todo> todos = await response.Content.ReadFromJsonAsync<List<Todo>>();
            return todos ?? new List<Todo>();
        }

        // GET /todos/{id}
        public async Task<Todo> GetByIdAsync(int id)
        {
            HttpResponseMessage response = await _client.GetAsync($"/todos/{id}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Todo>();
        }

        // POST /todos
        public async Task<Todo> CreateAsync(Todo todo)
        {
            HttpResponseMessage response = await _client.PostAsJsonAsync("/todos", todo);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Todo>();
        }

        // PATCH /todos/{id}
        public async Task<Todo> UpdateStatusAsync(int id, bool completed)
        {
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"/todos/{id}")
            {
                Content = JsonContent.Create(new { completed = completed })
            };

            HttpResponseMessage response = await _client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Todo>();
        }

        // DELETE /todos/{id}
        public async Task DeleteAsync(int id)
        {
            HttpResponseMessage response = await _client.DeleteAsync($"/todos/{id}");
            response.EnsureSuccessStatusCode();
        }
    }

    public class Program
    {
        public static async Task Main()
        {
            Console.OutputEncoding = Encoding.UTF8;

            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://jsonplaceholder.typicode.com");
                client.Timeout = TimeSpan.FromSeconds(15);

                var service = new TodoService(client);

                while (true)
                {
                    Console.WriteLine();
                    Console.WriteLine("1. Показать задачи пользователя");
                    Console.WriteLine("2. Найти задачу по ID");
                    Console.WriteLine("3. Создать задачу");
                    Console.WriteLine("4. Изменить статус задачи");
                    Console.WriteLine("5. Удалить задачу");
                    Console.WriteLine("0. Выход");
                    Console.WriteLine();
                    Console.Write("Выберите действие: ");

                    string choice = Console.ReadLine();
                    if (choice == "0")
                        break;

                    try
                    {
                        switch (choice)
                        {
                            case "1": await ShowUserTodosAsync(service); break;
                            case "2": await FindTodoAsync(service); break;
                            case "3": await CreateTodoAsync(service); break;
                            case "4": await UpdateStatusAsync(service); break;
                            case "5": await DeleteTodoAsync(service); break;
                            default: Console.WriteLine("Неизвестная команда."); break;
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine($"Ошибка HTTP-запроса: {ex.Message}");
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine("Превышено время ожидания ответа сервера.");
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"Ошибка разбора JSON: {ex.Message}");
                    }
                }
            }
        }

        private static int ReadInt(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                int value;
                if (int.TryParse(Console.ReadLine(), out value) && value > 0)
                    return value;
                Console.WriteLine("Введите положительное целое число.");
            }
        }

        private static bool ReadBool(string prompt)
        {
            while (true)
            {
                Console.Write(prompt);
                string s = (Console.ReadLine() ?? string.Empty).Trim().ToLower();
                if (s == "1" || s == "да") return true;
                if (s == "0" || s == "нет") return false;
                Console.WriteLine("Введите 1 (выполнена) или 0 (не выполнена).");
            }
        }

        private static async Task ShowUserTodosAsync(TodoService service)
        {
            int userId = ReadInt("Введите ID пользователя: ");
            List<Todo> todos = await service.GetByUserAsync(userId);

            if (todos.Count == 0)
            {
                Console.WriteLine("Задачи не найдены.");
                return;
            }

            foreach (Todo todo in todos)
                Console.WriteLine($"ID: {todo.Id} | {todo.Title} | {(todo.Completed ? "выполнена" : "не выполнена")}");
        }

        private static async Task FindTodoAsync(TodoService service)
        {
            int id = ReadInt("Введите ID задачи: ");
            Todo todo = await service.GetByIdAsync(id);
            Console.WriteLine(todo == null ? "Задача не найдена." : todo.ToString());
        }

        private static async Task CreateTodoAsync(TodoService service)
        {
            int userId = ReadInt("Введите ID пользователя: ");
            Console.Write("Введите название задачи: ");
            string title = (Console.ReadLine() ?? string.Empty).Trim();
            if (title.Length == 0)
            {
                Console.WriteLine("Название не может быть пустым.");
                return;
            }

            Todo created = await service.CreateAsync(new Todo
            {
                UserId = userId,
                Title = title,
                Completed = false
            });

            Console.WriteLine("Задача создана:");
            Console.WriteLine(created);
        }

        private static async Task UpdateStatusAsync(TodoService service)
        {
            int id = ReadInt("Введите ID задачи: ");
            bool completed = ReadBool("Новый статус (1 - выполнена, 0 - не выполнена): ");

            Todo updated = await service.UpdateStatusAsync(id, completed);
            Console.WriteLine("Статус изменён:");
            Console.WriteLine(updated);
        }

        private static async Task DeleteTodoAsync(TodoService service)
        {
            int id = ReadInt("Введите ID задачи: ");
            await service.DeleteAsync(id);
            Console.WriteLine($"Задача {id} удалена.");
        }
    }
}