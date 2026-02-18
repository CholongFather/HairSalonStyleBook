# HairSalonStyleBook 프로젝트 지침

## ⚠️ 핵심 컨텍스트 (반드시 숙지)

### 사용 대상
- **이 앱은 매장 관리자/직원 전용**이다. 일반 고객은 접근하지 않는다.
- 유일한 예외: `/pay` 페이지 (고객이 계좌정보 확인용, 2분 자동 만료)
- 고객 대면 기능이 아니므로 "고객 UX"보다 "관리자 편의성" 우선

### 인증 구조 (Admin / Viewer / 비인증)
- **Admin**: 비밀번호 로그인 → 스타일 CRUD, 매장설정, 갤러리 관리, 보안 로그
- **Viewer**: 간단 비밀번호 → 스타일북 열람, 갤러리 보기 (수정 불가)
- **비인증**: `/login`, `/pay` 페이지만 접근 가능
- `[Authorize]` 사용 시 `Microsoft.AspNetCore.Authorization` using 필요
- 인증은 client-side SHA256 + SessionStorage (서버 없음)

### 보안 / 감사 로그 규칙
- **모든 CRUD 작업 후 반드시 `AuditService.LogAsync()` 호출** (Create/Update/Delete/Publish)
- **토글 작업도 감사 로그 필수** (게시상태, 인기배지, 시그니처배지 등)
- 로그인 시도는 `LoginSecurityService`로 기록 (디바이스 핑거프린트, 실패 횟수)
- 5회 실패 → 30초 잠금, 관리자가 디바이스 영구 차단 가능
- HTML 출력 시 반드시 `HtmlSanitizer.Sanitize()` 사용 (XSS 방지)

### 반복 실수 방지
- Razor에서 `@((MarkupString)...)` 사용 금지 → `HtmlSanitizer.Sanitize()` 래핑 필수
- `@page` 예약어 때문에 Razor 내 `page` 변수명 사용 금지 → `pageNum` 등으로 대체
- Razor 미디어 쿼리: `@@media` (@ 이스케이프 필요)
- Razor에서 `&quot;` HTML 엔티티 대신 C# 메서드로 분리
- 비동기 토글 시 연타 방지 필수 (`HashSet<string>` 가드 패턴 사용)
- 파일 업로드: 5MB 초과 시 사용자에게 경고 표시 (silent skip 금지)
- Firestore 업데이트 후 `InvalidateCache()` 호출 필수

## 기술 스택
- **프레임워크**: Blazor WebAssembly (.NET 9)
- **UI**: Bootstrap 5 + 커스텀 CSS (MudBlazor 사용 안함)
- **데이터**: Firebase Firestore REST API (HttpClient 직접 호출)
- **이미지**: Firebase Storage REST API
- **인증**: 간단한 비밀번호 (client-side SHA256 비교, SessionStorage)
- **배포**: GitHub Pages (정적 호스팅)

## 프로젝트 구조
```
src/HairSalonStyleBook/
├── Models/          # 데이터 모델 (StylePost, ShopConfig 등)
├── Services/        # Firebase Firestore/Storage 서비스
├── Pages/           # Razor 페이지
│   ├── Admin/       # 관리자 페이지 (Dashboard, StyleEditor 등)
│   └── Home.razor   # 메인 페이지 (WiFi, 계좌, SNS 탭)
├── Components/      # 공통 컴포넌트 (ImageCarousel 등)
├── Auth/            # 인증 관련
└── wwwroot/         # 정적 파일
    └── appsettings.json  # Firebase 설정
```

## 코딩 규칙
- 한글 주석 사용
- CSS는 각 Razor 파일 내 `<style>` 블록에 작성 (scoped CSS 방식)
- Razor 미디어 쿼리: `@@media` (@ 이스케이프 필요)
- `@page` 예약어 때문에 Razor 내 `page` 변수명 사용 금지 → `pageNum` 등으로 대체
- Razor에서 `&quot;` HTML 엔티티 대신 C# 메서드로 분리

## Firebase 설정
- **Firestore 컬렉션**: `styles/`, `config/shop`, `auditLogs/`, `loginAttempts/`
- **Storage 경로**: `styles/{timestamp}_{filename}`
- 설정값: `wwwroot/appsettings.json` (ProjectId, ApiKey, StorageBucket)

## 스타일 데이터
- 총 96개 스타일 (커트/펌)
- 카테고리 5개: 남성 / 여성숏 / 여성단발 / 여성미디움 / 여성롱
- 이미지 앵글 4종: 정면 / 후면 / 측면 / 쿼터뷰
- 이미지는 Firebase Storage에 업로드 후 URL을 Firestore `imageUrls` 배열에 저장

## SNS QR 관리
- ShopConfig에서 각 SNS 활성/비활성 토글 (SnsInstagramEnabled, SnsKakaoEnabled 등)
- QR 코드는 `api.qrserver.com` API로 자동 생성
- 섹션: Instagram / 카카오톡 채널 / 네이버 (플레이스 + 리뷰)

## NanoBanana AI 이미지 생성
- 트리거 프롬프트: `docs/nanobanana-trigger-{angle}.txt` (front, back, quarter, side)
- 96개 스타일 x 4앵글 = 384장 목표
- 매 실행 10개씩 순서대로 처리
- 배경 7가지, 얼굴형 7가지 중 랜덤 선택
- NanoBanana가 프롬프트를 자주 무시하므로 결과물 반드시 검수 필요
