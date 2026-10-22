export type Category = { id: string; name: string };
export type ChainList = {
  id: string;
  name: string;
  categories: Category[];
  stepCount: number;
};
export type Step = {
  id: string;
  number: number;
  title: string | null;
  displayTitle: string;
  description: string;
  imageWarnings: string[];
};
export type Chain = {
  id: string;
  name: string;
  categories: Category[];
  steps: Step[];
};

export async function api<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const response = await fetch(`/api${path}`, {
    ...options,
    headers:
      options.body instanceof FormData
        ? options.headers
        : { "Content-Type": "application/json", ...options.headers },
  });
  if (!response.ok) {
    const body = (await response.json().catch(() => null)) as {
      message?: string;
    } | null;
    throw new Error(body?.message ?? `Ошибка запроса (${response.status})`);
  }
  return response.status === 204
    ? (undefined as T)
    : (response.json() as Promise<T>);
}

export const json = (body: unknown, method = "POST"): RequestInit => ({
  method,
  body: JSON.stringify(body),
});
export const message = (error: unknown) =>
  error instanceof Error ? error.message : "Неизвестная ошибка";
