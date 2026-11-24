export function getRoleHome(roles = []) {
    if (roles.includes("Admin")) return "/app/overview";
    if (roles.includes("OperationsDispatcher")) return "/app/overview";
    if (roles.includes("BookingCustomer")) return "/customer";
    if (roles.includes("ShipmentRecipient")) return "/recipient";

    return "/unauthorized";
}
