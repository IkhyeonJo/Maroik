import { describe, it, expect } from "vitest";
import { loadSite, antiForgery, hidden, hiddenByStyle } from "@tests/_common/harness";

// wwwroot/anonymous/custom/Account/js/site.js
function fixture(): string {
  return (
    antiForgery +
    hidden("returnUri", "/back") +
    `<a id="aChangeCultureEnUS"></a><a id="aChangeCultureKoKR"></a>
     <div id="loading" style="display:none"></div>
     <form id="loginForm"></form><form id="registerForm"></form>
     <form id="forgotPasswordForm"></form><form id="resetPasswordForm"></form>
     <button id="btnSignIn"></button><button id="btnRegister"></button>
     <button id="btnResend"></button><button id="btnChangePassword"></button>
     <button id="btnRequestNewPassword"></button>`
  );
}

describe("anonymous/Account", () => {
  it("switches culture via /Dashboard/CultureManagement", () => {
    const h = loadSite("anonymous", "Account", "", fixture());
    h.$("#aChangeCultureEnUS").trigger("click");
    expect(h.lastAjax().url).toBe("/Dashboard/CultureManagement");
    expect(JSON.parse(String(h.lastAjax().data))).toEqual({ Culture: "en-US" });
  });

  it("hides the loading spinner when the login form is invalid", () => {
    const h = loadSite("anonymous", "Account", "", fixture());
    (h.$.fn as any).valid = () => false;
    h.$("#loading").show();

    h.$("#btnSignIn").trigger("click");

    expect(hiddenByStyle(h.win.document.getElementById("loading"))).toBe(true);
  });
});
