
CREATE DATABASE DBGoRehab
USE DBGoRehab
/*
CREATE TABLE tblSeccionCuerpo 
(
Id int identity PRIMARY KEY, 
Nombre NVARCHAR(50) 
)


CREATE TABLE tblRutinaEjercicio
(
Id int identity PRIMARY KEY,
IdRutina int ,
IdEjercicio int , 
)



CREATE TABLE tblRutina
(
Id int identity PRIMARY KEY,
Nombre NVARCHAR(25) ,
Tipo NVARCHAR(25) ,
TiempoTotal float,
IdNivel int 
)


CREATE TABLE tblNivelDificultad
(
Id int identity PRIMARY KEY,
Nombre NVARCHAR(25) ,
)

DROP TABLE tblRutina
DROP TABLE tblSeccionCuerpo
DROP TABLE tblRutinaEjercicio
DROP TABLE tblNivelDificultad

alter table tblRutinaEjercicio add constraint FK_Ejercicio_Rutina
foreign key (IdEjercicio) references tblEjercicio  (Id)


alter table tblRutina add constraint FK_Rutina_Nivel
foreign key (IdNivel) references tblNivelDificultad  (Id)


alter table tblEjercicio add constraint FK_Ejercicio_Nivel
foreign key (IdNivel) references tblNivelDificultad  (Id)


alter table tblEjercicio add constraint FK_Ejercicio_SeccionCuerpo
foreign key (IdSeccionCuerpo) references tblSeccionCuerpo  (Id)

INSERT INTO tblEjercicio VALUES ('Esfinge','En posición boca abajo, colocar los antebrazos en el suelo y elevar la cabeza hacia atrás.',1,'https://youtu.be/uU6e4h_ViDU','http://fscomps.fotosearch.com/compc/LIF/LIF155/mm107002.jpg',5,1)
INSERT INTO tblEjercicio VALUES ('Mahometano','Con las rodillas dobladas extender el tronco hacia adelante y estirar los brazos.',1,'https://youtu.be/uU6e4h_ViDU','http://2.fimagenes.com/i/2/7/a1/am_79215_4145138_662240.jpg',10.5,2)
INSERT INTO tblEjercicio VALUES ('Press Sentado','Sentado en un banco, sosteniendo una mancuerna en cada mano asi como se observa a su izquierda.',3,'https://youtu.be/uU6e4h_ViDU','http://www.gimnasiototal.com/animaciones/ejercicios-de-hombros-1.gif',15.5,3)

INSERT INTO tblSeccionCuerpo VAlUES('Espalda'),('AnteBrazo'),('Hombro'),('Cuello')

INSERT INTO tblNivelDificultad VAlUES('Basico'),('Medio'),('Avanzado')

INSERT INTO tblRutina VALUES ('Espalda Basico',1,15.5,2)
INSERT INTO tblRutina VALUES ('Hombro Basico',1,15.5,2)

INSERT INTO tblRutinaEjercicio VALUES(1,4),(1,5),(2,6)


SELECT * FROM tblSeccionCuerpo
SELECT * FROM tblEjercicio
SELECT * FROM tblRutina
 SELECT * FROM tblRutinaEjercicio
 SELECT * FROM tblNivelDificultad
*/
----/// old//--


CREATE TABLE tblEjercicio
(
Id int identity PRIMARY KEY, 
Nombre NVARCHAR(50), 
Descripcion NVARCHAR(max) ,
SeccionCuerpo NVARCHAR(15) ,
URLVideoVimeo NVARCHAR(200) , 
URLImagen NVARCHAR(200) ,
Duracion float , 
Nivel NVARCHAR(25) 
)



CREATE TABLE tblRutina
(
Id INT IDENTITY PRIMARY KEY, 
IdPaciente INT, 
IdEjercicio INT,
)
alter table tblRutina add constraint FK_Rutina_Paciente
foreign key (IdPaciente) references tblPaciente (Id)
alter table tblRutina add constraint FK_Rutina_Ejercicio
foreign key (IdEjercicio) references tblEjercicio  (Id)


CREATE TABLE tblPaciente
(
Id int identity PRIMARY KEY, 
Estado NVARCHAR(100),
Incapacidad NVARCHAR(100),
FechaUltimoTratamiento DATETIME,
IdUsuario int 
)
alter table tblPaciente add constraint FK_Paciente_Usuario
foreign key (IdUsuario) references tblUsuario  (Id)


CREATE TABLE tblTerapeuta
(
Id int identity PRIMARY KEY, 
IdUsuario int ,
Especialidad NVARCHAR(10),
)
alter table tblTerapeuta add constraint FK_Terapeuta_Usuario
foreign key (IdUsuario) references tblUsuario  (Id)


CREATE TABLE tblUsuario
(
Id int identity PRIMARY KEY,
UserName NVARCHAR(10),
PrimerNombre NVARCHAR(25),
PrimerApellido NVARCHAR(20),
FechaIngreso DATETIME, 
Contrasena NVARCHAR(10),
)

alter table tblRutina add constraint FK_Rutina_Paciente
foreign key (IdPaciente) references tblPaciente  (Id)

alter table tblRutina add constraint FK_Ejercicio_Rutina
foreign key (IdEjercicio) references tblEjercicio  (Id)



SELECT * FROM tblEjercicio
SELECT * FROM tblPaciente
SELECT * FROM tblRutina
SELECT * FROM tblUsuario
SELECT * FROM tblTerapeuta

INSERT INTO tblEjercicio 
	VALUES ('Esfinje','En posición boca abajo, colocar los antebrazos en el suelo y elevar la cabeza hacia atrás.','Espalda','https://youtu.be/uU6e4h_ViDU','http://fscomps.fotosearch.com/compc/LIF/LIF155/mm107002.jpg',15.5,'Sencillo')
	,('Mahometano','Con las rodillas dobladas extender el tronco hacia adelante y estirar los brazos.','Espalda','https://youtu.be/uU6e4h_ViDU','http://2.fimagenes.com/i/2/7/a1/am_79215_4145138_662240.jpg',10.5,'Complejo')
,('Press Sentado','Sentado en un banco, sosteniendo una mancuerna en cada mano asi como se observa a su izquierda.','Hombro','https://youtu.be/uU6e4h_ViDU','http://www.gimnasiototal.com/animaciones/ejercicios-de-hombros-1.gif',15.5,'Sencillo')

INSERT INTO tblUsuario
VALUES('AlejandrHV','Alejandro','Herreno',GETDATE(),'bunny123')
INSERT INTO tblPaciente
VALUES('Iniciante','Extremidad Inferior Derecha',GETDATE(),4)

INSERT INTO tblUsuario
VALUES('SergioBun','Sergio','Bunny',GETDATE(),'bunny123')
INSERT INTO tblTerapeuta
VALUES(5,'Muscular')

SELECT * FROM tblPaciente
SELECT * FROM tblUsuario
SELECT * FROM tblEjercicio
SELECT * FROM tblRutina
SELECT * FROM tblTerapeuta
--DROP TABLE tblEjercicio
--DROP TABLE tblPaciente
--DROP TABLE tblRutina


