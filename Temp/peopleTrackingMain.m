%% This file is used for visually analyze doppler frequency shift

close all
clear all
clc

%% load data and format data
format long g; 
fid = fopen('Data\Doppler.txt');
Info = textscan(fid, '%f %f %f ','Delimiter',',');
fclose(fid);
portNumber=Info{1};
DFS=Info{2};
Timestamp=Info{3};

windowSize=500000;
timeMax=ceil(max(Timestamp/windowSize));
Data=zeros(4,timeMax);

for t=1:timeMax
    for a=1:4
        indexTime=find(Timestamp>=((t-1)*windowSize) & Timestamp<=(t*windowSize));
        indexPort=find(portNumber==a);
        index= intersect(indexTime,indexPort);
        if (index~=0)
            Data(a,t)=mean(DFS(index));
        end
        
        clear index;
        clear indexPort;
        clear indexTime;
    end
end


% Ant1=Data(1,:);
% Ant3=Data(3,:);
Ant1=smooth(Data(1,:),5);
Ant3=smooth(Data(3,:),5);


%% estimate moving trail
% stay still for first 5 seconds;
Ant1(1:5000000/windowSize)=0;
Ant3(1:5000000/windowSize)=0;

% setup the coordinates for antennas
xAnt1=0; yAnt1=400; r1=400;
xAnt3=-150; yAnt3=200; r3=250;
xStart=0; yStart=0;
Position=[];

% put first position into the list
Position=[Position;0,0];
syms newX newY x y;
lamda=30; % this is the estimated wavelength
for i=1:length(Ant1);
    fprintf('calculate point %d...\n',i);
    deltaR1= lamda*Ant1(i)*windowSize/(1000000*2);
    deltaR3= lamda*Ant3(i)*windowSize/(1000000*2);
    previousX=Position(size(Position,1),1);
    previousY=Position(size(Position,1),2);
    
    r1=r1-deltaR1;
    r3=r3-deltaR3;

    % solve for position      
    [newX,newY]=solve((x-xAnt1)^2+(y-yAnt1)^2-r1^2,(x-xAnt3)^2+(y-yAnt3)^2-r3^2);
    
    if (length(newX)==0)
        % use previous position
        Position=[Position;previousX,previousY];
    elseif (length(newX)==1)
        Position=[Position;newX,newY];
    elseif (pdist([double(newX(1)),double(newY(1));Position(i,1),Position(i,2)],'euclidean')<pdist([double(newX(2)),double(newY(2));Position(i,1),Position(i,2)],'euclidean'))
        Position=[Position;newX(1),newY(1)];
    else
        Position=[Position;newX(2),newY(2)];
    end
    
    figure(1)
    plot(Position(i+1,1),Position(i+1,2),'x-');
    axis([-200,200,-50,450]);
    hold on
    pause(0.2);
end

Position=double(Position);


