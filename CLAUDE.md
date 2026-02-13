# HairSalonStyleBook 프로젝트 지침

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
