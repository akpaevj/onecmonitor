import { apiPost } from "@/lib/api/client";

export type CompleteOAuthAuthorizeRequest = {
  clientId: string;
  redirectUri: string;
  codeChallenge: string;
  codeChallengeMethod: string;
  scope?: string | null;
  state?: string | null;
  deny?: boolean;
};

export type CompleteOAuthAuthorizeResponse = {
  redirectUrl: string;
};

export function completeOAuthAuthorize(request: CompleteOAuthAuthorizeRequest) {
  return apiPost<CompleteOAuthAuthorizeResponse, CompleteOAuthAuthorizeRequest>("/api/auth/oauth/authorize", request);
}
