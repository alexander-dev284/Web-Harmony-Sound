using HarmonySound.API.Consumer;
using HarmonySound.Models;
using HarmonySound.MVC.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace HarmonySound.MVC.Controllers
{
    public class PlansController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<PlansController> _logger;

        public PlansController(HttpClient httpClient, ILogger<PlansController> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        [Authorize(Roles = "client")]
        public async Task<IActionResult> Index()
        {
            try
            {
                var response = await _httpClient.GetAsync("https://localhost:7120/api/Plans");
                var json = await response.Content.ReadAsStringAsync();
                var plans = JsonSerializer.Deserialize<List<Plan>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                var premiumPlans = plans.Where(p => p.Price > 0).ToList();

                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                var userPlanResponse = await _httpClient.GetAsync($"https://localhost:7120/api/UserPlans/user/{userId}");

                UserPlan? userPlan = null;
                if (userPlanResponse.IsSuccessStatusCode)
                {
                    var userPlanJson = await userPlanResponse.Content.ReadAsStringAsync();
                    userPlan = JsonSerializer.Deserialize<UserPlan>(userPlanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                var viewModel = new SubscriptionViewModel
                {
                    CurrentPlan = userPlan?.Plan,
                    CurrentUserPlan = userPlan,
                    PremiumPlans = premiumPlans
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading plans");
                TempData["Error"] = "Error al cargar los planes";
                return View(new SubscriptionViewModel());
            }
        }

        [Authorize(Roles = "client")]
        [HttpPost]
        public async Task<IActionResult> Subscribe(int planId)
        {
            try
            {
                _logger.LogInformation($"Iniciando suscripción para plan {planId}");
                
                // Obtener información del plan
                var planResponse = await _httpClient.GetAsync($"https://localhost:7120/api/Plans/{planId}");
                if (!planResponse.IsSuccessStatusCode)
                {
                    _logger.LogError($"Plan {planId} no encontrado");
                    TempData["Error"] = "Plan no encontrado";
                    return RedirectToAction("Index");
                }

                var planJson = await planResponse.Content.ReadAsStringAsync();
                var plan = JsonSerializer.Deserialize<Plan>(planJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.LogInformation($"Plan encontrado: {plan.PlanName}, Precio: {plan.Price}");

                // Crear el pago con PayPal
                var paymentRequest = new
                {
                    Amount = plan.Price,
                    Currency = "USD",
                    Description = $"Suscripción a plan {plan.PlanName}"
                };

                var paymentContent = new StringContent(
                    JsonSerializer.Serialize(paymentRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                _logger.LogInformation($"Enviando solicitud de pago a PayPal...");
                
                var paymentResponse = await _httpClient.PostAsync("https://localhost:7120/api/PayPal/create-payment", paymentContent);
                var paymentResponseContent = await paymentResponse.Content.ReadAsStringAsync();

                _logger.LogInformation($"Respuesta PayPal: {paymentResponse.StatusCode}");
                _logger.LogInformation($"Contenido respuesta: {paymentResponseContent}");

                if (paymentResponse.IsSuccessStatusCode)
                {
                    var paymentResult = JsonSerializer.Deserialize<JsonElement>(paymentResponseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var approvalUrl = paymentResult.GetProperty("approvalUrl").GetString();
                    var paymentId = paymentResult.GetProperty("paymentId").GetString();

                    _logger.LogInformation($"Pago creado exitosamente: {paymentId}");
                    _logger.LogInformation($"URL de aprobación: {approvalUrl}");

                    // Guardar información en sesión para procesamiento posterior
                    HttpContext.Session.SetString("PaymentId", paymentId);
                    HttpContext.Session.SetInt32("PlanId", planId);
                    HttpContext.Session.SetInt32("UserId", int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value));

                    return Redirect(approvalUrl);
                }
                else
                {
                    _logger.LogError($"Error PayPal: {paymentResponse.StatusCode} - {paymentResponseContent}");
                    TempData["Error"] = $"Error al crear el pago con PayPal: {paymentResponse.StatusCode}";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PayPal payment");
                TempData["Error"] = $"Error al procesar el pago: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "client")]
        public async Task<IActionResult> PaymentSuccess(string paymentId, string PayerID)
        {
            try
            {
                // Recuperar información de la sesión
                var sessionPaymentId = HttpContext.Session.GetString("PaymentId");
                var planId = HttpContext.Session.GetInt32("PlanId");
                var userId = HttpContext.Session.GetInt32("UserId");

                if (sessionPaymentId != paymentId || !planId.HasValue || !userId.HasValue)
                {
                    TempData["Error"] = "Información de pago inválida";
                    return RedirectToAction("Index");
                }

                // Ejecutar el pago
                var executeRequest = new
                {
                    PaymentId = paymentId,
                    PayerId = PayerID
                };

                var executeContent = new StringContent(
                    JsonSerializer.Serialize(executeRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                var executeResponse = await _httpClient.PostAsync("https://localhost:7120/api/PayPal/execute-payment", executeContent);

                if (executeResponse.IsSuccessStatusCode)
                {
                    var executeJson = await executeResponse.Content.ReadAsStringAsync();
                    var executeResult = JsonSerializer.Deserialize<JsonElement>(executeJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    var paymentState = executeResult.GetProperty("state").GetString();
                    var amount = decimal.Parse(executeResult.GetProperty("amount").GetString());

                    if (paymentState == "approved")
                    {
                        // Procesar la suscripción
                        var subscriptionPayload = new
                        {
                            UserId = userId.Value,
                            PlanId = planId.Value,
                            Amount = amount,
                            PaymentMethod = "PayPal",
                            PayReference = paymentId,
                            PaymentState = "Success"
                        };

                        var subscriptionContent = new StringContent(
                            JsonSerializer.Serialize(subscriptionPayload),
                            Encoding.UTF8,
                            "application/json"
                        );

                        var subscriptionResponse = await _httpClient.PostAsync("https://localhost:7120/api/UserPlans/process-subscription", subscriptionContent);

                        if (subscriptionResponse.IsSuccessStatusCode)
                        {
                            // Limpiar sesión
                            HttpContext.Session.Remove("PaymentId");
                            HttpContext.Session.Remove("PlanId");
                            HttpContext.Session.Remove("UserId");

                            TempData["Success"] = "¡Suscripción exitosa! Pago procesado correctamente.";
                            return View("PaymentSuccess");
                        }
                        else
                        {
                            var errorContent = await subscriptionResponse.Content.ReadAsStringAsync();
                            _logger.LogError($"Subscription processing failed: {errorContent}");
                            TempData["Error"] = "Error al activar la suscripción";
                        }
                    }
                    else
                    {
                        TempData["Error"] = "El pago no fue aprobado";
                    }
                }
                else
                {
                    var errorContent = await executeResponse.Content.ReadAsStringAsync();
                    _logger.LogError($"PayPal payment execution failed: {errorContent}");
                    TempData["Error"] = "Error al ejecutar el pago";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment success");
                TempData["Error"] = $"Error procesando el pago: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "client")]
        public IActionResult PaymentCancel()
        {
            // Limpiar sesión
            HttpContext.Session.Remove("PaymentId");
            HttpContext.Session.Remove("PlanId");
            HttpContext.Session.Remove("UserId");

            TempData["Warning"] = "Pago cancelado por el usuario";
            return RedirectToAction("Index");
        }

        [Authorize(Roles = "client")]
        [HttpPost]
        public async Task<IActionResult> Cancel()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                var payload = new { UserId = userId };
                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("https://localhost:7120/api/UserPlans/cancel", content);

                if (response.IsSuccessStatusCode)
                    TempData["Success"] = "Suscripción cancelada exitosamente";
                else
                    TempData["Error"] = "Error al cancelar la suscripción";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling subscription");
                TempData["Error"] = "Error al cancelar la suscripción";
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "client")]
        public async Task<IActionResult> ManageInvitations()
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
                
                // Obtener plan actual del usuario
                var userPlanResponse = await _httpClient.GetAsync($"https://localhost:7120/api/UserPlans/user/{userId}");
                
                if (!userPlanResponse.IsSuccessStatusCode)
                {
                    TempData["Error"] = "No tienes un plan activo";
                    return RedirectToAction("Index");
                }

                var userPlanJson = await userPlanResponse.Content.ReadAsStringAsync();
                var userPlan = JsonSerializer.Deserialize<UserPlan>(userPlanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Solo el propietario del plan puede gestionar invitaciones
                // Verificar si el usuario actual pagó por el plan o solo fue invitado
                var isOwnerResponse = await _httpClient.GetAsync($"https://localhost:7120/api/UserPlans/is-plan-owner/{userId}");
                bool isPlanOwner = false;
                
                if (isOwnerResponse.IsSuccessStatusCode)
                {
                    var isOwnerJson = await isOwnerResponse.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(isOwnerJson);
                    isPlanOwner = result.GetProperty("isOwner").GetBoolean();
                }

                if (!isPlanOwner)
                {
                    TempData["Error"] = "Solo el propietario del plan puede gestionar invitaciones";
                    return RedirectToAction("Index");
                }

                // Obtener invitaciones con deserialización correcta
                List<InvitationDto> invitations = new List<InvitationDto>();
                
                var invitationsResponse = await _httpClient.GetAsync($"https://localhost:7120/api/PlanInvitations/sent/{userId}");
                
                if (invitationsResponse.IsSuccessStatusCode)
                {
                    var invitationsJson = await invitationsResponse.Content.ReadAsStringAsync();
                    invitations = JsonSerializer.Deserialize<List<InvitationDto>>(invitationsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new List<InvitationDto>();
                }

                // Contar correctamente las cuentas activas
                var activeInvitationsCount = invitations.Count(i => i.Status == "Accepted") + 1; // +1 por el propietario del plan
                ViewBag.ActiveInvitationsCount = activeInvitationsCount;
                ViewBag.CanSendMoreInvitations = activeInvitationsCount < userPlan.Plan.AccountLimit;
                ViewBag.IsPlanOwner = isPlanOwner;

                ViewBag.UserPlan = userPlan;
                ViewBag.Invitations = invitations;
                
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading invitations");
                TempData["Error"] = "Error al cargar las invitaciones";
                return RedirectToAction("Index");
            }
        }

        [Authorize(Roles = "client")]
        [HttpPost]
        public async Task<IActionResult> SendInvitation(string inviteeEmail, string message)
        {
            try
            {
                int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

                // Solo el propietario del plan puede enviar invitaciones
                var isOwnerResponse = await _httpClient.GetAsync($"https://localhost:7120/api/UserPlans/is-plan-owner/{userId}");
                
                if (!isOwnerResponse.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Error al verificar permisos";
                    return RedirectToAction("ManageInvitations");
                }

                var isOwnerJson = await isOwnerResponse.Content.ReadAsStringAsync();
                var ownerResult = JsonSerializer.Deserialize<JsonElement>(isOwnerJson);
                bool isPlanOwner = ownerResult.GetProperty("isOwner").GetBoolean();

                if (!isPlanOwner)
                {
                    TempData["Error"] = "Solo el propietario del plan puede enviar invitaciones";
                    return RedirectToAction("Index");
                }

                var payload = new
                {
                    InviterId = userId,
                    InviteeEmail = inviteeEmail,
                    Message = message
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("https://localhost:7120/api/PlanInvitations/send", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Invitación enviada exitosamente";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                    TempData["Error"] = errorObj.TryGetProperty("message", out var msg) ? msg.GetString() : "Error al enviar invitación";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending invitation");
                TempData["Error"] = "Error al enviar la invitación";
            }

            return RedirectToAction("ManageInvitations");
        }

        [HttpGet]
        public async Task<IActionResult> AcceptInvitation(string token)
        {
            try
            {
                var payload = new { Token = token };
                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("https://localhost:7120/api/PlanInvitations/accept", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<JsonElement>(responseJson);
                    
                    TempData["Success"] = "¡Invitación aceptada exitosamente! Ya tienes acceso premium.";
                    ViewBag.PlanName = result.GetProperty("planName").GetString();
                    ViewBag.InviterName = result.GetProperty("inviterName").GetString();
                    
                    return View("InvitationAccepted");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                    TempData["Error"] = errorObj.TryGetProperty("message", out var msg) ? msg.GetString() : "Error al aceptar invitación";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting invitation");
                TempData["Error"] = "Error al procesar la invitación";
            }

            return RedirectToAction("Index");
        }

        [Authorize(Roles = "client")]
        [HttpPost]
        public async Task<IActionResult> CancelInvitation(int invitationId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"https://localhost:7120/api/PlanInvitations/cancel/{invitationId}", null);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Invitación cancelada exitosamente";
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                    TempData["Error"] = errorObj.TryGetProperty("message", out var msg) ? msg.GetString() : "Error al cancelar invitación";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling invitation");
                TempData["Error"] = "Error al cancelar la invitación";
            }

            return RedirectToAction("ManageInvitations");
        }

        public ActionResult Details(int id)
        {
            var data = Crud<Plan>.GetById(id);
            return View(data);
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Plan data)
        {
            try
            {
                Crud<Plan>.Create(data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        public ActionResult Edit(int id)
        {
            var data = Crud<Plan>.GetById(id);
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, Plan data)
        {
            try
            {
                Crud<Plan>.Update(id, data);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }

        public ActionResult Delete(int id)
        {
            var data = Crud<Plan>.GetById(id);
            return View(data);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, Plan data)
        {
            try
            {
                Crud<Plan>.Delete(id);
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View(data);
            }
        }
    }
}
