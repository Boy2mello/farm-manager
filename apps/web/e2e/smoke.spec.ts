import { test, expect } from "@playwright/test";

test.describe("Smoke — phone viewport", () => {
  test("home page renders the brand + primary CTA", async ({ page }) => {
    await page.goto("/");
    await expect(page.getByRole("heading", { name: "Farm Manager" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Sign in" })).toBeVisible();
  });

  test("login page enforces email validation", async ({ page }) => {
    await page.goto("/login");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page.getByText("Enter a valid email")).toBeVisible();
  });
});

test.describe("Mobile-first guarantees", () => {
  test("touch targets are at least 44px tall on the login form", async ({ page }) => {
    await page.goto("/login");
    const submit = page.getByRole("button", { name: "Sign in" });
    const box = await submit.boundingBox();
    expect(box).not.toBeNull();
    expect(box!.height).toBeGreaterThanOrEqual(44);
  });

  test("layout fits the device viewport without horizontal scroll", async ({ page }) => {
    await page.goto("/");
    const overflow = await page.evaluate(() => {
      const html = document.documentElement;
      return html.scrollWidth > html.clientWidth + 1;
    });
    expect(overflow).toBeFalsy();
  });
});
