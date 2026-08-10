# PointingKeys

키보드로 글을 작성하다가 마우스로 손 왔다갔다하는 밤티나는 행동을 하지 않아도 되도록 하는 windows 트레이 프로그램입니다.
이 프로그램은 CapsLock 적극 활용하며 본래 기능은 CapsLock의 본래 기능은 홍명보처럼 사라졌으니 주의하시길 바랍니다.

## 주요 키

- 마우스 모드 활성화 : CapsLock
- 마우스 포인터 이동 : I J K L
- 좌클릭 : 3 / Space / E / D
- 우클릭 : 4 / R / F / 좌·우 Alt
- 위로 스크롤 : H
- 아래로 스크롤 : N

## 요구사항

- Windows 10/11 (누구와 달리 TPM 2.0 없어도 됨)
- CapsLock 입력이 되는 키보드 / 환경

## 빌드

하지마세요 Github Release에 산출물 있어요.

하고싶으면 :

```powershell
dotnet build -c Release
dotnet run -c Release
```

## 자동 시작 등록

Windows 시작 시 자동 실행하려면 Github Release 최신버전을 다운로드, 압축 해제 후
아무곳에나 프로그램을 저장 한 뒤, `PointingKeys.exe`의 바로가기를 아래 폴더에 넣으세요.

```
%APPDATA%\Microsoft\Windows\Start Menu\Programs\Startup
```